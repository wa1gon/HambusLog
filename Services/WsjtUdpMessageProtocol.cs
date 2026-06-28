namespace HamBusLog.Services;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Low-level parser for the WSJT-X UDP Message Protocol envelope.
/// </summary>
public static class WsjtUdpMessageProtocol
{
    public const uint Magic = 0xADBCCBDA;

    public static bool TryParse(byte[] datagram, out WsjtUdpEnvelope envelope)
    {
        envelope = default!;

        if (datagram is null || datagram.Length < 16)
            return false;

        try
        {
            var span = datagram.AsSpan();
            var offset = 0;

            var magic = ReadUInt32(span, ref offset);
            if (magic != Magic)
                return false;

            var schemaVersion = (int)ReadUInt32(span, ref offset);
            var messageType = ReadMessageType(span, ref offset);
            var clientId = ReadNetworkString(span, ref offset);
            var payload = span[offset..].ToArray();

            envelope = new WsjtUdpEnvelope(magic, schemaVersion, messageType, clientId, payload, datagram);
            ConsumeEnvelope(envelope);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ConsumeEnvelope(WsjtUdpEnvelope envelope)
    {
        _ = envelope.Magic;
        _ = envelope.SchemaVersion;
        _ = envelope.MessageType;
        _ = envelope.ClientId;
        _ = envelope.Payload;
        _ = envelope.RawDatagram;
    }

    private static WsjtMessageType ReadMessageType(ReadOnlySpan<byte> span, ref int offset)
    {
        var value = (int)ReadUInt32(span, ref offset);
        return Enum.IsDefined(typeof(WsjtMessageType), value)
            ? (WsjtMessageType)value
            : WsjtMessageType.Unknown;
    }

    internal static string ReadNetworkString(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length)
            return string.Empty;

        var len = BinaryPrimitives.ReadInt32BigEndian(span[offset..(offset + 4)]);
        offset += 4;
        if (len <= 0 || offset + len > span.Length)
            return string.Empty;

        var s = Encoding.UTF8.GetString(span[offset..(offset + len)]);
        offset += len;
        return s;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) throw new InvalidDataException("Truncated uint32.");
        var v = BinaryPrimitives.ReadUInt32BigEndian(span[offset..(offset + 4)]);
        offset += 4;
        return v;
    }
}

public sealed record WsjtUdpEnvelope(
    uint Magic,
    int SchemaVersion,
    WsjtMessageType MessageType,
    string ClientId,
    byte[] Payload,
    byte[] RawDatagram);


