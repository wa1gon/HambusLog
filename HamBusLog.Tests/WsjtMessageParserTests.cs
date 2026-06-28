using System.Buffers.Binary;
using System.Text;
using HamBusLog.Services;
using HamBusLog.ViewModels;

namespace HamBusLog.Tests;

public sealed class WsjtMessageParserTests
{
    [Fact]
    public void TryParse_LoggedAdifPacket_ExtractsClientAndAdif()
    {
        var parser = new WsjtMessageParser();
        var adif = "<CALL:5>K1ABC <QSO_DATE:8>20260624 <TIME_ON:6>123045 <BAND:3>20M <MODE:4>MFSK <SUBMODE:3>FT8 <FREQ:6>14.074 <RST_SENT:2>-6 <RST_RCVD:3>-12 <EOR>";
        var packet = BuildPacket(12, "WSJT-X", adif);

        var ok = parser.TryParse(packet, out var parsed);

        Assert.True(ok);
        Assert.Equal(WsjtMessageType.LoggedAdif, parsed.MessageType);
        Assert.Equal("WSJT-X", parsed.ClientId);
        Assert.Equal(adif, parsed.LoggedAdif);
    }

    [Fact]
    public void TryBuildLoggedQso_ParsesImportantFields()
    {
        var parser = new WsjtMessageParser();
        var adif = "<CALL:5>K1ABC <QSO_DATE:8>20260624 <TIME_ON:6>123045 <BAND:3>20M <MODE:4>MFSK <SUBMODE:3>FT8 <FREQ:6>14.074 <RST_SENT:2>-6 <RST_RCVD:3>-12 <GRIDSQUARE:4>FN31 <STATE:2>ma <CNTY:3>MID <EOR>";

        var ok = parser.TryBuildLoggedQso(adif, out var qso);

        Assert.True(ok);
        Assert.Equal("K1ABC", qso.Call);
        Assert.Equal("20M", qso.Band);
        Assert.Equal("MFSK", qso.Mode);
        Assert.Equal("FT8", qso.Submode);
        Assert.Equal("14.074", qso.FreqMhz);
        Assert.Equal("-6", qso.RstSent);
        Assert.Equal("-12", qso.RstRcvd);
        Assert.Equal("FN31", qso.GridSquare);
        Assert.Equal("MA", qso.State);
        Assert.Equal("MID", qso.County);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 12, 30, 45, TimeSpan.Zero), qso.TimeOnUtc);
    }

    [Fact]
    public void ApplyWsjtLoggedQso_PopulatesLogInputFields()
    {
        using var vm = new LogInputViewModel();
        var qso = new WsjtLoggedQso(
            "<EOR>",
            "K1ABC",
            new DateTimeOffset(2026, 6, 24, 12, 30, 45, TimeSpan.Zero),
            "20M",
            "MFSK",
            "FT8",
            "-5",
            "-8",
            "14.074",
            "FN31",
            "FN42",
            "MA",
            "MID",
            "USA",
            "DARRYL",
            "K1OWN",
            "K1OP",
            "MA");

        vm.ApplyWsjtLoggedQso(qso);

        Assert.Equal("K1ABC", vm.InputCall);
        Assert.Equal("20260624", vm.InputDate);
        Assert.Equal("1230", vm.InputTimeOn);
        Assert.Equal("20M", vm.InputBand);
        Assert.Equal("FT8", vm.InputMode);
        Assert.Equal("14.074", vm.InputFreq);
        Assert.Equal("-5", vm.InputSent);
        Assert.Equal("-8", vm.InputRec);
        Assert.Equal("FN31", vm.InputGrid);
        Assert.Equal("MA", vm.InputState);
        Assert.Equal("MID", vm.InputCounty);
        Assert.Equal("USA", vm.InputCountry);
        Assert.Equal("K1OP", vm.InputOperator);
    }

    [Fact]
    public void TryParse_DecodePacket_WithPlaceholderMode_DoesNotEmitModeTilde()
    {
        var parser = new WsjtMessageParser();
        var packet = BuildDecodePacket("WSJT-X", mode: "~", message: "CQ K1ABC FN31", lowConfidence: false);

        var ok = parser.TryParse(packet, out var parsed);

        Assert.True(ok);
        Assert.Equal(WsjtMessageType.Decode, parsed.MessageType);
        Assert.DoesNotContain("mode=~", parsed.DecodedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mode=", parsed.DecodedText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CQ K1ABC FN31", parsed.DecodedText, StringComparison.Ordinal);
    }

    private static byte[] BuildPacket(int messageType, string clientId, string payloadText)
    {
        var clientBytes = Encoding.UTF8.GetBytes(clientId);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadText);

        var packet = new byte[4 + 4 + 4 + 4 + clientBytes.Length + 4 + payloadBytes.Length];
        var offset = 0;

        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), 0xADBCCBDA);
        offset += 4;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), 3);
        offset += 4;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), (uint)messageType);
        offset += 4;
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(offset, 4), clientBytes.Length);
        offset += 4;
        clientBytes.CopyTo(packet.AsSpan(offset));
        offset += clientBytes.Length;
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(offset, 4), payloadBytes.Length);
        offset += 4;
        payloadBytes.CopyTo(packet.AsSpan(offset));

        return packet;
    }

    private static byte[] BuildDecodePacket(string clientId, string mode, string message, bool lowConfidence)
    {
        var clientBytes = Encoding.UTF8.GetBytes(clientId);
        var modeBytes = Encoding.UTF8.GetBytes(mode);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        var packet = new byte[
            4 + 4 + 4 +
            4 + clientBytes.Length +
            1 +
            4 +
            4 +
            8 +
            4 +
            4 + modeBytes.Length +
            4 + messageBytes.Length +
            1];

        var offset = 0;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), 0xADBCCBDA);
        offset += 4;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), 3);
        offset += 4;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), (uint)WsjtMessageType.Decode);
        offset += 4;

        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(offset, 4), clientBytes.Length);
        offset += 4;
        clientBytes.CopyTo(packet.AsSpan(offset));
        offset += clientBytes.Length;

        packet[offset++] = 1; // isNew
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), 12_345);
        offset += 4;
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(offset, 4), -7);
        offset += 4;

        var deltaTBits = BitConverter.DoubleToInt64Bits(0.3);
        BinaryPrimitives.WriteInt64BigEndian(packet.AsSpan(offset, 8), deltaTBits);
        offset += 8;

        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(offset, 4), 1234);
        offset += 4;

        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(offset, 4), modeBytes.Length);
        offset += 4;
        modeBytes.CopyTo(packet.AsSpan(offset));
        offset += modeBytes.Length;

        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(offset, 4), messageBytes.Length);
        offset += 4;
        messageBytes.CopyTo(packet.AsSpan(offset));
        offset += messageBytes.Length;

        packet[offset] = lowConfidence ? (byte)1 : (byte)0;
        return packet;
    }
}



