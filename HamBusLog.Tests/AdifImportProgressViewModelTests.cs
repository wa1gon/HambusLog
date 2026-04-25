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

        viewModel.Update(new AdifImportProgress(stage, "/tmp/sample.adi", "status", 42, 7, false, 1d));

        Assert.Equal(expectedLabel, viewModel.RecordCounterLabel);
        Assert.Equal("42", viewModel.RecordCounterValueText);
    }
}


