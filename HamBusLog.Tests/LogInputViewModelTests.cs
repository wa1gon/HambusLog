using HamBusLog.Hardware;
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
        Assert.Equal("14.074000", viewModel.InputFreq);
        Assert.Equal("20M", viewModel.InputBand);

        viewModel.SelectedConnectedRadio = CreateOption("Slice B", "Flex Slice B", "CW", 7_030_000);

        Assert.Equal("CW", viewModel.InputMode);
        Assert.Equal("7.030000", viewModel.InputFreq);
        Assert.Equal("40M", viewModel.InputBand);
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
    public void SelectingRadio_PreservesHundredHertzPrecision()
    {
        var viewModel = new LogInputViewModel();

        viewModel.SelectedConnectedRadio = CreateOption("Slice C", "Flex Slice C", "USB", 14_280_100);

        Assert.Equal("14.280100", viewModel.InputFreq);
    }

    private static ConnectedRadioOption CreateOption(string radioName, string label, string mode, long frequencyHz)
    {
        return new ConnectedRadioOption(new RadioRuntimeState(
            radioName,
            label,
            true,
            mode,
            2400,
            frequencyHz,
            null,
            DateTime.UtcNow));
    }
}

