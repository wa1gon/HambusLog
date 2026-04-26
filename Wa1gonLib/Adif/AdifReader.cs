

namespace HamBusLog.Wa1gonLib.Adif;
public class AdifReader
{
    private static readonly Regex AdifFieldPattern = new(@"<([^:>]+):(\d+)(:[^>]*)?>([^<]*)", RegexOptions.IgnoreCase);
    public static readonly string Direct = "CARD";
    public static readonly string Lotw = "LOTW";
    public static readonly string Eqsl = "EQSL";

    // Todo: Add read from stream
    public static List<Qso> ReadFromFile(string filePath)
    {
        // string content = File.ReadAllText(filePath);
        // if (string.IsNullOrWhiteSpace(content))
        //     throw new ArgumentException("File content cannot be null or empty.", nameof(filePath));
        // var qsos = ReadFromString(content);
        var qsos = ReadFromAdifFile(filePath);
        return qsos;
    }

    /// <summary>
    ///     /This method reads ADIF content from a string and returns a list of QSO objects.
    ///     However, it requites more memory than reading from a file.
    /// </summary>
    /// <param name="content"></param>
    /// <returns>List&gt;Qso&lt;</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public static List<Qso> ReadFromString(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));


        // Skip to after the <EOH> tag
        var eohIndex = content.IndexOf("<EOH>", StringComparison.OrdinalIgnoreCase);
        if (eohIndex < 0)
            throw new InvalidDataException("Missing <EOH> tag.");

        var qsoBlock = content.Substring(eohIndex + 5);
        var records = Regex.Split(qsoBlock, "<EOR>", RegexOptions.IgnoreCase);

        var qsos = new List<Qso>();

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record)) continue;

            var fields = ParseFields(record);
            var qso = ParseQso(fields);
            if (qso.Id == Guid.Empty)
                qso.Id = Guid.NewGuid(); // Ensure every QSO has a unique ID
            qsos.Add(qso);
        }

        return qsos;
    }

    /// <summary>
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static List<Qso> ReadFromAdifFile(string filePath)
    {
        var qsos = new List<Qso>();
        using var reader = new StreamReader(filePath);

        // Read until <EOH>
        string? line;
        var headerBuilder = new StringBuilder();
        while ((line = reader.ReadLine()) != null)
        {
            headerBuilder.AppendLine(line);
            if (line.IndexOf("<EOH>", StringComparison.OrdinalIgnoreCase) >= 0)
                break;
        }

        if (line == null)
            throw new InvalidDataException("Missing <EOH> tag.");

        // Read and process records one at a time
        var recordBuilder = new StringBuilder();
        while ((line = reader.ReadLine()) != null)
        {
            recordBuilder.AppendLine(line);
            if (line.IndexOf("<EOR>", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var record = recordBuilder.ToString();
                recordBuilder.Clear();
                if (!string.IsNullOrWhiteSpace(record))
                {
                    var fields = ParseFields(record);
                    var qso = ParseQso(fields);
                    if (qso.Id == Guid.Empty)
                        qso.Id = Guid.NewGuid();
                    qsos.Add(qso);
                }
            }
        }

        return qsos;
    }

    private static Dictionary<string, string> ParseFields(string record)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AdifFieldPattern.Matches(record))
        {
            var fieldName = match.Groups[1].Value.Trim();
            var length = int.Parse(match.Groups[2].Value.Trim());
            var value = match.Groups[4].Value.Trim();

            if (value.Length > length)
                value = value.Substring(0, length);

            fields[fieldName] = value;
        }

        return fields;
    }

    private static Qso ParseQso(Dictionary<string, string> fields)
    {
        var qso = new Qso();
        var qslInfos = new Dictionary<string, QsoQslInfo>(StringComparer.OrdinalIgnoreCase);
        DateTime? qsoDate = null;
        TimeSpan? qsoTime = null;

        foreach (var field in fields)
        {
            var name = field.Key.ToLowerInvariant();
            var value = field.Value;

            var service = (name.Contains("lotw") ? "LOTW"
                : name.Contains("eqsl") ? "EQSL"
                : name.StartsWith("qsl_") ? "DIRECT" : null) ?? string.Empty;
            QsoQslInfo? qslInfo = null;
            if (service != null)
                if (!qslInfos.TryGetValue(service, out qslInfo))
                {
                    qslInfo = new QsoQslInfo { QslService = service };
                    qslInfos[service] = qslInfo;
                }

            switch (name)
            {
                // Core fields
                case "call": qso.Call = value; break;
                case "band": qso.Band = value; break;
                case "mode": qso.Mode = value; break;
                case "country": qso.Country = value; break;
                case "state": qso.State = value; break;
                case "dxcc": qso.Dxcc = int.TryParse(value, out var val) ? val : 0; break;
                case "rst_sent": qso.RstSent = value; break;
                case "rst_rcvd": qso.RstRcvd = value; break;
                case "contest-id": qso.ContestId = value; break;
                case "guid": qso.Id = Guid.Parse(value); break;
                case "freq":
                    if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var freq))
                        qso.Freq = freq;
                    break;
                case "qso_date":
                    if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out var date))
                        qsoDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    break;
                case "time_on":
                    var formats = value.Length == 4 ? new[] { "HHmm" } : new[] { "HHmmss" };
                    if (TimeSpan.TryParseExact(value, formats, CultureInfo.InvariantCulture, out var time))
                        qsoTime = time;
                    break;

                // QSL fields
                case var n when n == "lotw_qsl_sent":
                    if (qslInfos.TryGetValue("LOTW", out qslInfo)) qslInfo.QslSent = value == "Y";
                    break;
                case var n when n == "lotw_qsl_rcvd":
                    if (qslInfos.TryGetValue("LOTW", out qslInfo)) qslInfo.QslReceived = value == "Y";
                    break;
                case var n when n == "eqsl_qsl_sent":
                    if (qslInfos.TryGetValue("EQSL", out qslInfo)) qslInfo.QslSent = value == "Y";
                    break;
                case var n when n == "eqsl_qsl_rcvd":
                    if (qslInfos.TryGetValue("EQSL", out qslInfo)) qslInfo.QslReceived = value == "Y";
                    break;
                case var n when n == "qsl_sent":
                    if (qslInfos.TryGetValue("DIRECT", out qslInfo)) qslInfo.QslSent = value == "Y";
                    break;
                case var n when n == "qsl_rcvd":
                    if (qslInfos.TryGetValue("DIRECT", out qslInfo)) qslInfo.QslReceived = value == "Y";
                    break;

                default:
                    qso.Details.Add(new QsoDetail
                    {
                        FieldName = field.Key,
                        FieldValue = value
                    });
                    break;
            }
        }

        if (qso.Id == Guid.Empty)
            qso.Id = Guid.NewGuid();

        if (qsoDate.HasValue)
        {
            var dateTime = qsoDate.Value.Date;
            if (qsoTime.HasValue)
                dateTime = dateTime.Add(qsoTime.Value);

            qso.QsoDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        else
        {
            qso.QsoDate = DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc);
        }

        foreach (var qslInfo in qslInfos.Values) qslInfo.QsoId = qso.Id;
        qso.QslInfo = qslInfos.Values.ToList();

        return qso;
    }
}