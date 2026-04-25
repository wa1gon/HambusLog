using HamBusLog.Data;
using HamBusLog.ViewModels;
using Xunit;

namespace HamBusLog.Tests;

public sealed class AdifImportProgressViewModelTests
{
    [Theory]
    [InlineData(AdifImportStage.Scanning, "Records found so far")]
    [InlineData(AdifImportStage.Parsing, "Records parsed")]
    [InlineData(AdifImportStage.Saving, "Records to import")]
    [InlineData(AdifImportStage.Completed, "Records imported")]
    public void Update_SetsStageAwareRecordCounterLabel(AdifImportStage stage, string expectedLabel)
    {
        var viewModel = new AdifImportProgressViewModel();

        viewModel.Update(new AdifImportProgress(
            stage,
            "/tmp/sample.adi",
            "status",
            RecordsRead: 42,
            TotalRecords: null,
            SavedChanges: 7,
            IsIndeterminate: false,
            ProgressFraction: 1d));

        Assert.Equal(expectedLabel, viewModel.RecordCounterLabel);
        Assert.Equal("42", viewModel.RecordCounterValueText);
    }

    [Fact]
    public void Update_ShowsCurrentOverTotal_WhenTotalRecordsAreKnown()
    {
        var viewModel = new AdifImportProgressViewModel();

        viewModel.Update(new AdifImportProgress(
            AdifImportStage.Saving,
            "/tmp/sample.adi",
            "status",
            RecordsRead: 42,
            TotalRecords: 120,
            SavedChanges: 0,
            IsIndeterminate: true,
            ProgressFraction: 0.5d));

        Assert.Equal("42 / 120", viewModel.RecordCounterValueText);
        Assert.Equal("Records to import: 42 / 120", viewModel.RecordsText);
    }
}




