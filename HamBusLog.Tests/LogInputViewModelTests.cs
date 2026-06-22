using HamBusLog.Hardware;
using HamBusLog.Services;
using HamBusLog.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HamBusLog.Tests;

public sealed class LogInputViewModelTests
{
    [Fact]
    public void SelectingDifferentRadio_AutoUpdatesLogFields()
    {
        var viewModel = new LogInputViewModel();

        viewModel.SelectedConnectedRadio = CreateOption("Slice A", "Flex Slice A", "USB", 14_074_000);

        Assert.Equal("USB", viewModel.InputMode);
        Assert.Equal("14.074000", viewModel.InputFreq);
        Assert.Equal("20M", viewModel.InputBand);

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "CW", 7_030_000);

        Assert.Equal("CW", viewModel.InputMode);
        Assert.Equal("7.030000", viewModel.InputFreq);
        Assert.Equal("40M", viewModel.InputBand);
    }

    [Fact]
    public void RefreshingSameSelectedRadio_OverwritesEditedFieldsFromActiveRadio()
    {
        var viewModel = new LogInputViewModel();

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "USB", 14_074_000);
        viewModel.InputMode = "DIGU";
        viewModel.InputFreq = "14.095";
        viewModel.InputBand = "20M";

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "AM", 3_885_000);
        viewModel.ApplySelectedRadioToInputs();

        Assert.Equal("AM", viewModel.InputMode);
        Assert.Equal("3.885000", viewModel.InputFreq);
        Assert.Equal("80M", viewModel.InputBand);
    }

    [Fact]
    public void SelectingRadio_PopulatesFrequencyAndBandFromRigState()
    {
        var viewModel = new LogInputViewModel();

        viewModel.SelectedConnectedRadio = CreateOption("Slice C", "Flex Slice C", "USB", 14_280_100);

        Assert.Equal("14.280100", viewModel.InputFreq);
        Assert.Equal("20M", viewModel.InputBand);
    }

    [Fact]
    public void RefreshAutoFields_UpdatesTimeOn()
    {
        var viewModel = new LogInputViewModel
        {
            InputTimeOn = "9999"
        };

        viewModel.RefreshAutoFields();

        Assert.NotEqual("9999", viewModel.InputTimeOn);
        Assert.Matches("^[0-9]{4}$", viewModel.InputTimeOn);
    }

    [Fact]
    public void CallSectionAndClass_AreForcedToUppercase()
    {
        var viewModel = new LogInputViewModel();

        viewModel.InputCall = "wa1gon";
        viewModel.InputFieldDaySection = "ema";
        viewModel.InputFieldDayClass = "1d";

        Assert.Equal("WA1GON", viewModel.InputCall);
        Assert.Equal("EMA", viewModel.InputFieldDaySection);
        Assert.Equal("1D", viewModel.InputFieldDayClass);
    }

    [Fact]
    public void PrepareForNextLogEntry_ClearsCallSectionAndClassOnly()
    {
        var viewModel = new LogInputViewModel
        {
            InputCall = "WA1GON",
            InputFieldDaySection = "EMA",
            InputFieldDayClass = "1D",
            InputMode = "USB",
            InputBand = "20M",
            InputFreq = "14.280100"
        };

        viewModel.PrepareForNextLogEntry();

        Assert.Equal(string.Empty, viewModel.InputCall);
        Assert.Equal(string.Empty, viewModel.InputFieldDaySection);
        Assert.Equal(string.Empty, viewModel.InputFieldDayClass);
        Assert.Equal("USB", viewModel.InputMode);
        Assert.Equal("20M", viewModel.InputBand);
        Assert.Equal("14.280100", viewModel.InputFreq);
    }

    [Fact]
    public void TryBuildQso_NormalContest_UsesDefaultRstOnly()
    {
        var call = FindUnusedCallsign();
        var viewModel = new LogInputViewModel
        {
            SelectedContestType = ContestType.Normal,
            InputCall = call,
            InputDate = "20260505",
            InputTimeOn = "1930",
            InputBand = "20M",
            InputMode = "SSB",
            InputSent = "59",
            InputRec = "59",
            InputExchange = "AR",
            InputCountry = "USA",
            InputState = "AR",
            InputCounty = "PUL",
            InputName = "TEST"
        };

        var qso = viewModel.TryBuildQso(out var error);

        Assert.NotNull(qso);
        Assert.Equal(string.Empty, error);
        Assert.Equal("NORMAL", qso!.ContestId);
        Assert.Equal("59", qso.RstSent);
        Assert.Equal("59", qso.RstRcvd);
    }

    [Fact]
    public void TryBuildQso_FieldDay_RequiresSectionAndClassOnly()
    {
        var call = FindUnusedCallsign();
        var viewModel = new LogInputViewModel
        {
            SelectedContestType = ContestType.ArrlFieldDay,
            InputCall = call,
            InputDate = "20260505",
            InputTimeOn = "1930",
            InputBand = "20M",
            InputMode = "CW",
            InputFieldDaySection = "EMA",
            InputFieldDayClass = "1D",
            InputExchange = "EMA",
            InputCountry = "USA",
            InputState = "MA"
        };

        var qso = viewModel.TryBuildQso(out var error);

        Assert.NotNull(qso);
        Assert.Equal(string.Empty, error);
        Assert.Equal("ARRL-FIELD-DAY", qso!.ContestId);
        Assert.Equal(string.Empty, qso!.RstSent);
        Assert.Equal(string.Empty, qso.RstRcvd);
        Assert.Contains(qso.Details, d => d.FieldName == "Section" && d.FieldValue == "EMA");
        Assert.Contains(qso.Details, d => d.FieldName == "Class" && d.FieldValue == "1D");
    }

    [Fact]
    public void TryBuildQso_DefaultNormalLog_AddsCommentDetailFromNotes()
    {
        var call = FindUnusedCallsign();
        var viewModel = new LogInputViewModel
        {
            SelectedContestType = ContestType.Normal,
            InputCall = call,
            InputDate = "20260505",
            InputTimeOn = "1930",
            InputBand = "20M",
            InputMode = "SSB",
            InputSent = "59",
            InputRec = "59",
            InputExchange = "AR",
            InputCountry = "USA",
            InputState = "AR",
            InputCounty = "PUL",
            InputName = "TEST",
            InputNormalNotes = "Worked mobile from a park"
        };

        var qso = viewModel.TryBuildQso(out var error);

        Assert.NotNull(qso);
        Assert.Equal(string.Empty, error);
        Assert.Contains(qso!.Details, d => d.FieldName == "COMMENT" && d.FieldValue == "Worked mobile from a park");
    }

    [Fact]
    public void PrepareForNextLogEntry_ClearsDefaultNormalLogNotes()
    {
        var viewModel = new LogInputViewModel
        {
            InputNormalNotes = "Temporary note"
        };

        viewModel.PrepareForNextLogEntry();

        Assert.Equal(string.Empty, viewModel.InputNormalNotes);
    }

    [Fact]
    public void AvailableModes_ContainsExpectedModes()
    {
        var viewModel = new LogInputViewModel();
        var expectedModes = new[] { "USB", "LSB", "FM", "AM", "FT8", "FT4", "RTTY", "PSK31", "PSK63", 
                                    "OLIVIA", "JS8", "MFSK", "PACKET", "HELL", "THOR", "DOMINO", "DIGITAL", "CW" };

        Assert.All(expectedModes, mode => Assert.Contains(mode, viewModel.AvailableModes));
    }

    [Fact]
    public void InputMode_ForcesUppercase()
    {
        var viewModel = new LogInputViewModel();

        // Test lowercase
        viewModel.InputMode = "ssb";
        Assert.Equal("SSB", viewModel.InputMode);

        // Test mixed case
        viewModel.InputMode = "CW";
        Assert.Equal("CW", viewModel.InputMode);

        // Test lowercase cw
        viewModel.InputMode = "cw";
        Assert.Equal("CW", viewModel.InputMode);

        // Test empty string
        viewModel.InputMode = string.Empty;
        Assert.Equal(string.Empty, viewModel.InputMode);
    }

    private static ConnectedRadioOption CreateOption(string radioName, string label, string mode, long frequencyHz)
    {
        return new ConnectedRadioOption(new RadioRuntimeState(
            radioName,
            label,
            true,
            mode,
            frequencyHz / 1_000_000m,
            null,
            DateTime.UtcNow));
    }

    private static string FindUnusedCallsign()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (var i = 0; i < letters.Length; i++)
        for (var j = 0; j < letters.Length; j++)
        for (var k = 0; k < letters.Length; k++)
        {
            var candidate = $"K9{letters[i]}{letters[j]}{letters[k]}";
            var exists = App.DbContext.Qsos.AsNoTracking().Any(q => q.Call.ToUpper() == candidate);
            if (!exists)
                return candidate;
        }

        return "K9ZZZ";
    }
}
