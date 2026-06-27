using System.Reflection;
using HamBusLog.Services;
using HamBusLog.ViewModels;
using HamBusLog.Wa1gonLib.Models;
using Xunit;

namespace HamBusLog.Tests;

public sealed class WsjtAutoLogFieldDayTests
{
    [Fact]
    public void BuildQsoFromWsjt_FieldDay_StoresClassAndSectionInDetails_NotRst()
    {
        var wsjt = BuildLoggedQso(rstSent: "1D", rstRcvd: "EMA", exchange: "1D EMA");
        var contest = ContestCatalog.Get(ContestType.ArrlFieldDay);

        var qso = BuildQsoViaReflection(wsjt, contest);

        Assert.Equal(string.Empty, qso.RstSent);
        Assert.Equal(string.Empty, qso.RstRcvd);
        Assert.Contains(qso.Details, d => d.FieldName == "Class" && d.FieldValue == "1D");
        Assert.Contains(qso.Details, d => d.FieldName == "Section" && d.FieldValue == "EMA");
    }

    [Fact]
    public void BuildQsoFromWsjt_NormalContest_KeepsRstFields()
    {
        var wsjt = BuildLoggedQso(rstSent: "-05", rstRcvd: "-08", exchange: "");
        var contest = ContestCatalog.Get(ContestType.Normal);

        var qso = BuildQsoViaReflection(wsjt, contest);

        Assert.Equal("-05", qso.RstSent);
        Assert.Equal("-08", qso.RstRcvd);
    }

    [Fact]
    public void BuildQsoFromWsjt_FieldDay_InvalidExchange_DoesNotPopulateClassSection()
    {
        var wsjt = BuildLoggedQso(rstSent: "-10", rstRcvd: "-12", exchange: "RR73");
        var contest = ContestCatalog.Get(ContestType.ArrlFieldDay);

        var qso = BuildQsoViaReflection(wsjt, contest);

        Assert.Equal(string.Empty, qso.RstSent);
        Assert.Equal(string.Empty, qso.RstRcvd);
        Assert.DoesNotContain(qso.Details, d => d.FieldName == "Class");
        Assert.DoesNotContain(qso.Details, d => d.FieldName == "Section");
    }

    private static Qso BuildQsoViaReflection(WsjtLoggedQso wsjt, ContestDefinition contest)
    {
        var method = typeof(App).GetMethod("BuildQsoFromWsjt", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var value = method!.Invoke(null, [wsjt, contest]);
        var qso = Assert.IsType<Qso>(value);
        return qso;
    }

    private static WsjtLoggedQso BuildLoggedQso(string rstSent, string rstRcvd, string exchange)
    {
        return new WsjtLoggedQso(
            RawAdif: "<EOR>",
            Call: "K1ABC",
            TimeOnUtc: new DateTimeOffset(2026, 6, 27, 18, 30, 0, TimeSpan.Zero),
            Band: "20M",
            Mode: "MFSK",
            Submode: "FT8",
            RstSent: rstSent,
            RstRcvd: rstRcvd,
            FreqMhz: "14.074",
            GridSquare: "FN31",
            MyGridSquare: "FN42",
            State: "MA",
            County: "MID",
            Country: "USA",
            Name: "DARRYL",
            StationCallsign: "WA1GON",
            Operator: "WA1GON",
            ExchangeReceived: exchange);
    }
}


