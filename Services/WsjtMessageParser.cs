namespace HamBusLog.Services;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Decodes WSJT-X UDP Message Protocol datagrams (schema 2/3) into structured messages.
/// Wire format: big-endian. QByteArray = int32-length + UTF-8 bytes (-1 = null/empty).
/// bool = 1 byte, quint32/qint32 = 4 bytes, quint64 = 8 bytes, double = 8 bytes IEEE-754.
/// QDateTime = qint64 Julian-day (8) + quint32 msecs-since-midnight (4) + quint8 spec (1);
///             if spec==2 an additional qint32 UTC-offset follows.
/// </summary>
public sealed class WsjtMessageParser
{
    public bool TryParse(byte[] datagram, out WsjtParsedMessage parsed)
    {
        parsed = new WsjtParsedMessage(WsjtMessageType.Unknown, 0, string.Empty,
            "Invalid packet", string.Empty, datagram, string.Empty);

        if (!WsjtUdpMessageProtocol.TryParse(datagram, out var packet))
            return false;

        try
        {
            var (summary, decoded, loggedAdif) = DecodeByType(packet.MessageType, packet.SchemaVersion, packet.Payload.AsSpan(), 0);

            parsed = new WsjtParsedMessage(packet.MessageType, packet.SchemaVersion, packet.ClientId,
                summary, decoded, packet.RawDatagram, loggedAdif);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Per-type dispatcher ───────────────────────────────────────────────────

    private static (string Summary, string Decoded, string LoggedAdif) DecodeByType(
        WsjtMessageType type, int schema, ReadOnlySpan<byte> span, int offset)
    {
        try
        {
            return type switch
            {
                WsjtMessageType.Heartbeat  => DecodeHeartbeat(span, offset),
                WsjtMessageType.Status     => DecodeStatus(span, offset),
                WsjtMessageType.Decode     => DecodeDecodeMsg(span, offset),
                WsjtMessageType.Clear      => DecodeClear(span, offset, schema),
                WsjtMessageType.QsoLogged  => DecodeQsoLogged(span, offset),
                WsjtMessageType.LoggedAdif => DecodeLoggedAdif(span, offset),
                _                          => (type.ToString(), string.Empty, string.Empty)
            };
        }
        catch
        {
            return (type.ToString(), string.Empty, string.Empty);
        }
    }

    // ── Type-specific decoders ────────────────────────────────────────────────

    private static (string, string, string) DecodeHeartbeat(ReadOnlySpan<byte> span, int offset)
    {
        var maxSchema = ReadUInt32(span, ref offset);
        var version   = ReadNetworkString(span, ref offset);
        var revision  = ReadNetworkString(span, ref offset);
        var decoded   = $"ver={version}  rev={revision}  max-schema={maxSchema}";
        return ("Heartbeat", decoded, string.Empty);
    }

    private static (string, string, string) DecodeStatus(ReadOnlySpan<byte> span, int offset)
    {
        var dialHz       = ReadUInt64(span, ref offset);
        var mode         = ReadNetworkString(span, ref offset);
        var dxCall       = ReadNetworkString(span, ref offset);
        var report       = ReadNetworkString(span, ref offset);
        var transmitting = ReadBool(span, ref offset);
        var decoding     = ReadBool(span, ref offset);
        var rxDf         = ReadUInt32(span, ref offset);
        var txDf         = ReadUInt32(span, ref offset);
        var deCall       = ReadNetworkString(span, ref offset);
        var deGrid       = ReadNetworkString(span, ref offset);
        var dxGrid       = ReadNetworkString(span, ref offset);
        /*txWatchdog*/    ReadBool(span, ref offset);
        var subMode      = ReadNetworkString(span, ref offset);

        // schema >= 3 optional tail: fast(bool) + specOp(byte) + freqTol(u32) + trPeriod(u32)
        // + configName + txMessage — tolerant reads
        string configName = string.Empty, txMessage = string.Empty;
        if (TryReadBool(span, ref offset) &&
            TryReadByte(span, ref offset)  &&
            TryReadUInt32(span, ref offset) &&
            TryReadUInt32(span, ref offset))
        {
            configName = TryReadNetworkString(span, ref offset);
            txMessage  = TryReadNetworkString(span, ref offset);
        }

        var modeStr = FormatMode(mode, subMode);
        var freqStr = dialHz > 0 ? $"{dialHz / 1_000_000.0:F3} MHz" : "?";
        var txTag   = transmitting ? "  [TX]"    : string.Empty;
        var dcTag   = decoding     ? "  [DECODE]" : string.Empty;
        var dxPart  = string.IsNullOrWhiteSpace(dxCall) ? string.Empty : $"  dx={dxCall}/{dxGrid}";
        var cfgPart = string.IsNullOrWhiteSpace(configName) ? string.Empty : $"  cfg={configName}";
        var msgPart = string.IsNullOrWhiteSpace(txMessage)  ? string.Empty : $"  msg={txMessage}";
        var modePart = string.IsNullOrWhiteSpace(modeStr) ? string.Empty : $"  mode={modeStr}";

        var summary = $"Status  {deCall}  {freqStr}{txTag}{dcTag}".Trim();
        var decoded = $"de={deCall}/{deGrid}  freq={freqStr}{modePart}  " +
                      $"rxDF={rxDf}  txDF={txDf}  rpt={report}" +
                      $"{dxPart}{txTag}{dcTag}{cfgPart}{msgPart}";
        return (summary, decoded.Trim(), string.Empty);
    }

    private static (string, string, string) DecodeDecodeMsg(ReadOnlySpan<byte> span, int offset)
    {
        /*isNew*/         ReadBool(span, ref offset);
        var timeMs  = ReadUInt32(span, ref offset);
        var snr     = ReadInt32(span, ref offset);
        var deltaT  = ReadDouble(span, ref offset);
        var deltaF  = ReadUInt32(span, ref offset);
        var mode    = ReadNetworkString(span, ref offset);
        var message = ReadNetworkString(span, ref offset);
        var lowConf = ReadBool(span, ref offset);

        var timeStr = TimeSpan.FromMilliseconds(timeMs).ToString(@"hh\:mm\:ss");
        var snrStr  = snr >= 0 ? $"+{snr}" : snr.ToString(CultureInfo.InvariantCulture);
        var lcTag   = lowConf ? "  [LC]" : string.Empty;
        var modeText = NormalizeToken(mode);
        var modePart = string.IsNullOrWhiteSpace(modeText) ? string.Empty : $"  mode={modeText}";

        var summary = string.IsNullOrWhiteSpace(message) ? "Decode" : message.Trim();
        var decoded = $"[{timeStr}]  snr={snrStr}  dt={deltaT:+0.0;-0.0}s  " +
                      $"df={deltaF}Hz{modePart}{lcTag}  |  {message}";
        return (summary, decoded.Trim(), string.Empty);
    }

    private static (string, string, string) DecodeClear(
        ReadOnlySpan<byte> span, int offset, int schema)
    {
        var window = schema >= 3 && offset < span.Length ? span[offset] : (byte)0;
        return ("Clear", window > 0 ? $"clear  window={window}" : "clear", string.Empty);
    }

    private static (string, string, string) DecodeQsoLogged(ReadOnlySpan<byte> span, int offset)
    {
        SkipQDateTime(span, ref offset);               // date_time_off
        var dxCall   = ReadNetworkString(span, ref offset);
        var dxGrid   = ReadNetworkString(span, ref offset);
        var txFreqHz = ReadUInt64(span, ref offset);
        var mode     = ReadNetworkString(span, ref offset);
        var rptSent  = ReadNetworkString(span, ref offset);
        var rptRcvd  = ReadNetworkString(span, ref offset);
        var txPower  = ReadNetworkString(span, ref offset);
        var comments = ReadNetworkString(span, ref offset);
        var name     = ReadNetworkString(span, ref offset);
        SkipQDateTime(span, ref offset);               // date_time_on
        var opCall   = TryReadNetworkString(span, ref offset);
        var myCall   = TryReadNetworkString(span, ref offset);
        var myGrid   = TryReadNetworkString(span, ref offset);

        var normalizedMode = NormalizeToken(mode);
        var freqStr  = txFreqHz > 0 ? $"{txFreqHz / 1_000_000.0:F3} MHz" : string.Empty;
        var summary  = string.IsNullOrWhiteSpace(dxCall) ? "QsoLogged" : $"QSO logged: {dxCall}";
        var decoded  = $"dx={dxCall}/{dxGrid}  freq={freqStr}" +
                       (string.IsNullOrWhiteSpace(normalizedMode) ? "  " : $"  mode={normalizedMode}  ") +
                       $"sent={rptSent}  rcvd={rptRcvd}";
        if (!string.IsNullOrWhiteSpace(name))     decoded += $"  name={name}";
        if (!string.IsNullOrWhiteSpace(opCall))   decoded += $"  op={opCall}";
        if (!string.IsNullOrWhiteSpace(myCall))   decoded += $"  my={myCall}/{myGrid}";
        if (!string.IsNullOrWhiteSpace(txPower))  decoded += $"  pwr={txPower}";
        if (!string.IsNullOrWhiteSpace(comments)) decoded += $"  comments={comments}";
        return (summary, decoded.Trim(), string.Empty);
    }

    private static (string, string, string) DecodeLoggedAdif(ReadOnlySpan<byte> span, int offset)
    {
        var rawAdif = ReadNetworkString(span, ref offset).Trim();
        var fields  = ParseAdifFields(rawAdif);

        var call    = ReadField(fields, "CALL").ToUpperInvariant();
        var band    = ReadField(fields, "BAND").ToUpperInvariant();
        var mode    = FormatMode(ReadField(fields, "MODE"), ReadField(fields, "SUBMODE"));
        var date    = ReadField(fields, "QSO_DATE");
        var time    = ReadField(fields, "TIME_ON");
        var freq    = ReadField(fields, "FREQ");
        var rstS    = ReadField(fields, "RST_SENT");
        var rstR    = ReadField(fields, "RST_RCVD");
        var grid    = ReadField(fields, "GRIDSQUARE").ToUpperInvariant();
        var state   = ReadField(fields, "STATE").ToUpperInvariant();
        var country = ReadField(fields, "COUNTRY").ToUpperInvariant();

        var summary = string.IsNullOrWhiteSpace(call)
            ? "Logged ADIF"
            : $"Logged ADIF: {call}  {band}  {mode}";

        var parts = new List<string>();
        void Add(string k, string v) { if (!string.IsNullOrWhiteSpace(v)) parts.Add($"{k}={v}"); }
        Add("CALL", call); Add("DATE", date); Add("TIME", time);
        Add("BAND", band); Add("FREQ", freq); Add("MODE", mode);
        Add("RST_S", rstS); Add("RST_R", rstR); Add("GRID", grid);
        Add("STATE", state); Add("COUNTRY", country);

        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "CALL","QSO_DATE","TIME_ON","BAND","FREQ","MODE","SUBMODE",
              "RST_SENT","RST_RCVD","GRIDSQUARE","STATE","COUNTRY" };
        foreach (var kv in fields)
            if (!shown.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                parts.Add($"{kv.Key.ToUpperInvariant()}={kv.Value}");

        return (summary, string.Join("  ", parts), rawAdif);
    }

    // ── ADIF parsing ──────────────────────────────────────────────────────────

    public bool TryBuildLoggedQso(string rawAdif, out WsjtLoggedQso qso)
    {
        var fields = ParseAdifFields(rawAdif);
        var call   = ReadField(fields, "CALL");

        if (string.IsNullOrWhiteSpace(call))
        {
            qso = new WsjtLoggedQso(rawAdif, string.Empty, null,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            return false;
        }

        qso = new WsjtLoggedQso(
            rawAdif,
            call.Trim().ToUpperInvariant(),
            ParseUtc(ReadField(fields, "QSO_DATE"), ReadField(fields, "TIME_ON")),
            NormalizeToken(ReadField(fields, "BAND")),
            NormalizeToken(ReadField(fields, "MODE")),
            NormalizeToken(ReadField(fields, "SUBMODE")),
            ReadField(fields, "RST_SENT"),
            ReadField(fields, "RST_RCVD"),
            ReadField(fields, "FREQ"),
            NormalizeToken(ReadField(fields, "GRIDSQUARE")),
            NormalizeToken(ReadField(fields, "MY_GRIDSQUARE")),
            NormalizeToken(ReadField(fields, "STATE")),
            NormalizeToken(ReadField(fields, "CNTY")),
            NormalizeToken(ReadField(fields, "COUNTRY")),
            ReadField(fields, "NAME"),
            NormalizeToken(ReadField(fields, "STATION_CALLSIGN")),
            NormalizeToken(ReadField(fields, "OPERATOR")),
            NormalizeToken(ReadField(fields, "SRX_STRING")));
        return true;
    }

    internal static Dictionary<string, string> ParseAdifFields(string rawAdif)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawAdif)) return fields;

        var text  = rawAdif.Trim();
        var index = 0;
        while (index < text.Length)
        {
            var start = text.IndexOf('<', index);
            if (start < 0) break;
            var end = text.IndexOf('>', start + 1);
            if (end < 0) break;

            var header = text[(start + 1)..end].Trim();
            index = end + 1;
            if (header.Equals("EOR", StringComparison.OrdinalIgnoreCase)) break;

            var parts = header.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var len) || len < 0) continue;
            if (index >= text.Length) break;
            var take  = Math.Min(len, text.Length - index);
            fields[parts[0].Trim().ToUpperInvariant()] = text.Substring(index, take).Trim();
            index += take;
        }
        return fields;
    }

    internal static string TryGetAdifField(string rawAdif, string key)
    {
        if (string.IsNullOrWhiteSpace(rawAdif) || string.IsNullOrWhiteSpace(key))
            return string.Empty;
        return ReadField(ParseAdifFields(rawAdif), key);
    }

    // ── Formatting ────────────────────────────────────────────────────────────

    private static string FormatMode(string mode, string submode)
    {
        var m = NormalizeToken(mode);
        var s = NormalizeToken(submode);
        if (string.IsNullOrWhiteSpace(s) || string.Equals(m, s, StringComparison.OrdinalIgnoreCase))
            return m;
        return $"{m}/{s}";
    }

    private static string ReadField(IReadOnlyDictionary<string, string> f, string key)
        => f.TryGetValue(key, out var v) ? v.Trim() : string.Empty;

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToUpperInvariant();
        return normalized == "~" ? string.Empty : normalized;
    }

    private static DateTimeOffset? ParseUtc(string adifDate, string adifTime)
    {
        var d = adifDate.Trim();
        var t = adifTime.Trim();
        if (d.Length != 8 || t.Length < 4) return null;
        if (t.Length > 6) t = t[..6];
        if (t.Length == 4) t += "00";
        if (DateTime.TryParseExact(d + t, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return new DateTimeOffset(parsed, TimeSpan.Zero);
        return null;
    }

    // ── Binary primitives ─────────────────────────────────────────────────────

    private static uint ReadUInt32(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) throw new InvalidDataException("Truncated uint32.");
        var v = BinaryPrimitives.ReadUInt32BigEndian(span[offset..(offset + 4)]);
        offset += 4; return v;
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 8 > span.Length) throw new InvalidDataException("Truncated uint64.");
        var v = BinaryPrimitives.ReadUInt64BigEndian(span[offset..(offset + 8)]);
        offset += 8; return v;
    }

    private static int ReadInt32(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) throw new InvalidDataException("Truncated int32.");
        var v = BinaryPrimitives.ReadInt32BigEndian(span[offset..(offset + 4)]);
        offset += 4; return v;
    }

    private static double ReadDouble(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 8 > span.Length) throw new InvalidDataException("Truncated double.");
        var bits = BinaryPrimitives.ReadInt64BigEndian(span[offset..(offset + 8)]);
        offset += 8; return BitConverter.Int64BitsToDouble(bits);
    }

    private static bool ReadBool(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset >= span.Length) throw new InvalidDataException("Truncated bool.");
        return span[offset++] != 0;
    }

    private static bool TryReadBool(ReadOnlySpan<byte> span, ref int offset)
    { if (offset >= span.Length) return false; _ = span[offset++] != 0; return true; }

    private static bool TryReadByte(ReadOnlySpan<byte> span, ref int offset)
    { if (offset >= span.Length) return false; _ = span[offset++]; return true; }

    private static bool TryReadUInt32(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) return false;
        _ = BinaryPrimitives.ReadUInt32BigEndian(span[offset..(offset + 4)]);
        offset += 4;
        return true;
    }

    internal static string ReadNetworkString(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) return string.Empty;
        var len = BinaryPrimitives.ReadInt32BigEndian(span[offset..(offset + 4)]);
        offset += 4;
        if (len <= 0 || offset + len > span.Length) return string.Empty;
        var s = Encoding.UTF8.GetString(span[offset..(offset + len)]);
        offset += len; return s;
    }

    private static string TryReadNetworkString(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) return string.Empty;
        try { return ReadNetworkString(span, ref offset); }
        catch { return string.Empty; }
    }

    /// <summary>
    /// Skip a Qt5 QDateTime: 8 bytes Julian-day-number + 4 bytes msecs + 1 byte time-spec.
    /// If time-spec == 2 (OffsetFromUTC) an extra 4-byte offset follows.
    /// </summary>
    private static void SkipQDateTime(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 13 > span.Length) return;
        offset += 12;                      // Julian day (8) + msecs (4)
        var spec = span[offset++];         // time-spec (1)
        if (spec == 2 && offset + 4 <= span.Length)
            offset += 4;
    }
}



