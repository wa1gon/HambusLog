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

    [Fact]
    public void Qso_ComputedProperties_ResolveFieldDayClassAndSection()
    {
        var qso = new Qso
        {
            Id = Guid.NewGuid(),
            Call = "K1ABC",
            StationCallSign = "WA1GON",
            QsoDate = DateTime.Now,
            Band = "20M",
            Mode = "FT8",
            Details = new List<QsoDetail>
            {
                new() { FieldName = "Section", FieldValue = "EMA" },
                new() { FieldName = "Class", FieldValue = "1D" }
            }
        };

        Assert.Equal("EMA", qso.FieldDaySection);
        Assert.Equal("1D", qso.FieldDayClass);
    }

    [Fact]
    public void BuildWsjtDuplicateWarningMessage_FormatsUserVisibleToastText()
    {
        var wsjt = BuildLoggedQso(rstSent: "-05", rstRcvd: "-08", exchange: "");
        var contest = ContestCatalog.Get(ContestType.Normal);
        var qso = BuildQsoViaReflection(wsjt, contest);

        var method = typeof(App).GetMethod("BuildWsjtDuplicateWarningMessage", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var value = method!.Invoke(null, [qso, contest]);
        var message = Assert.IsType<string>(value);

        Assert.Equal("Skipped duplicate K1ABC on 20M FT8 (Normal).", message);
    }

    [Fact]
    public void ShouldShowWsjtDuplicateToast_ThrottlesRepeatedDuplicateNotifications()
    {
        var wsjt = BuildLoggedQso(rstSent: "-05", rstRcvd: "-08", exchange: "");
        var contest = ContestCatalog.Get(ContestType.Normal);
        var qso = BuildQsoViaReflection(wsjt, contest);

        var method = typeof(App).GetMethod("ShouldShowWsjtDuplicateToast", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var first = Assert.IsType<bool>(method!.Invoke(null, [qso, contest, new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc)]));
        var second = Assert.IsType<bool>(method.Invoke(null, [qso, contest, new DateTime(2026, 6, 28, 12, 0, 5, DateTimeKind.Utc)]));
        var third = Assert.IsType<bool>(method.Invoke(null, [qso, contest, new DateTime(2026, 6, 28, 12, 0, 16, DateTimeKind.Utc)]));

        Assert.True(first);
        Assert.False(second);
        Assert.True(third);
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



