using HamBusLog.Hardware;
using HamBusLog.Services;
using HamBusLog.ViewModels;
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
        // FrequencyMhz is not populated from radio state, so frequency remains empty
        Assert.Equal(string.Empty, viewModel.InputFreq);
        // Band cannot be derived without frequency
        Assert.Equal(string.Empty, viewModel.InputBand);

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "CW", 7_030_000);

        Assert.Equal("CW", viewModel.InputMode);
    }

    [Fact]
    public void RefreshingSameSelectedRadio_DoesNotOverwriteEditedFields()
    {
        var viewModel = new LogInputViewModel();

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "USB", 14_074_000);
        viewModel.InputMode = "DIGU";
        viewModel.InputFreq = "14.095";
        viewModel.InputBand = "20M";

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "AM", 3_885_000);

        Assert.Equal("DIGU", viewModel.InputMode);
        Assert.Equal("14.095", viewModel.InputFreq);
        Assert.Equal("20M", viewModel.InputBand);
    }

    [Fact]
    public void SelectingRadio_DoesNotOverwriteManuallyEnteredFrequency()
    {
        var viewModel = new LogInputViewModel();

        viewModel.SelectedConnectedRadio = CreateOption("Slice C", "Flex Slice C", "USB", 14_280_100);

        // FrequencyMhz is not populated from radio state
        Assert.Equal(string.Empty, viewModel.InputFreq);
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
    public void TryBuildQso_NormalContest_RequiresExtendedExchange()
    {
        var viewModel = new LogInputViewModel
        {
            SelectedContestType = ContestType.Normal,
            InputCall = "K1ABC",
            InputDate = "20260505",
            InputTimeOn = "1930",
            InputBand = "20M",
            InputMode = "SSB"
        };

        var qso = viewModel.TryBuildQso(out var error);

        Assert.Null(qso);
        Assert.Contains("RST Sent", error, StringComparison.OrdinalIgnoreCase);

        viewModel.InputSent = "59";
        viewModel.InputRec = "59";
        viewModel.InputCountry = "USA";
        viewModel.InputName = "Pat";
        viewModel.InputState = "MA";
        viewModel.InputCounty = "MIDDLESEX";

        qso = viewModel.TryBuildQso(out error);

        Assert.NotNull(qso);
        Assert.Equal(string.Empty, error);
        Assert.Equal("USA", qso!.Country);
        Assert.Equal("MA", qso.State);
        Assert.Contains(qso.Details, d => d.FieldName == "Name" && d.FieldValue == "Pat");
        Assert.Contains(qso.Details, d => d.FieldName == "County" && d.FieldValue == "MIDDLESEX");
    }

    [Fact]
    public void TryBuildQso_FieldDay_RequiresSectionAndClassOnly()
    {
        var viewModel = new LogInputViewModel
        {
            SelectedContestType = ContestType.ArrlFieldDay,
            InputCall = "K1ABC",
            InputDate = "20260505",
            InputTimeOn = "1930",
            InputBand = "20M",
            InputMode = "CW",
            InputFieldDaySection = "EMA",
            InputFieldDayClass = "1D"
        };

        var qso = viewModel.TryBuildQso(out var error);

        Assert.NotNull(qso);
        Assert.Equal(string.Empty, error);
        Assert.Equal(string.Empty, qso!.RstSent);
        Assert.Equal(string.Empty, qso.RstRcvd);
        Assert.Contains(qso.Details, d => d.FieldName == "Section" && d.FieldValue == "EMA");
        Assert.Contains(qso.Details, d => d.FieldName == "Class" && d.FieldValue == "1D");
    }

    private static ConnectedRadioOption CreateOption(string radioName, string label, string mode, long frequencyHz)
    {
        return new ConnectedRadioOption(new RadioRuntimeState(
            radioName,
            label,
            true,
            mode,
            null,
            DateTime.UtcNow));
    }
}

