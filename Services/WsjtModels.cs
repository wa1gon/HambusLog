namespace HamBusLog.Services;

public enum WsjtMessageType
{
    Heartbeat = 0,
    Status = 1,
    Decode = 2,
    Clear = 3,
    Reply = 4,
    QsoLogged = 5,
    Close = 6,
    Replay = 7,
    HaltTx = 8,
    FreeText = 9,
    WsprDecode = 10,
    Location = 11,
    LoggedAdif = 12,
    HighlightCallsign = 13,
    SwitchConfiguration = 14,
    Configure = 15,
    TxMessage = 16,
    Unknown = 999
}

public sealed record WsjtTrafficEvent(
    DateTimeOffset TimestampUtc,
    string Direction,
    WsjtMessageType MessageType,
    string ClientId,
    string Summary,
    string DecodedText,
    byte[] Payload);

public sealed record WsjtLoggedQso(
    string RawAdif,
    string Call,
    DateTimeOffset? TimeOnUtc,
    string Band,
    string Mode,
    string Submode,
    string RstSent,
    string RstRcvd,
    string FreqMhz,
    string GridSquare,
    string MyGridSquare,
    string State,
    string County,
    string Country,
    string Name,
    string StationCallsign,
    string Operator,
    string ExchangeReceived);

public sealed record WsjtParsedMessage(
    WsjtMessageType MessageType,
    int SchemaVersion,
    string ClientId,
    string Summary,
    string DecodedText,
    byte[] Payload,
    string LoggedAdif);



