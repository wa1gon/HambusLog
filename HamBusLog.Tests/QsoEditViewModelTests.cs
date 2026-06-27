using HamBusLog.ViewModels;
using HamBusLog.Wa1gonLib.Models;
using Xunit;

namespace HamBusLog.Tests;

public sealed class QsoEditViewModelTests
{
    [Fact]
    public void LoadFrom_PopulatesSectionAndClassFromAliases()
    {
        var qso = new Qso
        {
            Id = Guid.NewGuid(),
            Call = "WA1GON",
            Band = "20M",
            Mode = "SSB",
            QsoDate = DateTime.UtcNow,
            StationCallSign = "WA1GON",
            Details =
            [
                new QsoDetail { FieldName = "ARRL_SECT", FieldValue = "EMA" },
                new QsoDetail { FieldName = "FD_CLASS", FieldValue = "1D" },
                new QsoDetail { FieldName = "COUNTY", FieldValue = "PUL" }
            ]
        };

        var viewModel = new QsoEditViewModel();

        viewModel.LoadFrom(qso);

        Assert.Equal("EMA", viewModel.Section);
        Assert.Equal("1D", viewModel.QsoClass);
        Assert.DoesNotContain(viewModel.Details, x => string.Equals(x.FieldName, "ARRL_SECT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(viewModel.Details, x => string.Equals(x.FieldName, "FD_CLASS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildUpdatedQso_WritesCanonicalSectionAndClassDetails()
    {
        var id = Guid.NewGuid();
        var viewModel = new QsoEditViewModel
        {
            Call = "wa1gon",
            Band = "20m",
            Mode = "ssb",
            QsoDateText = "2026-06-01 12:30",
            StationCallSign = "wa1gon",
            Section = "eny",
            QsoClass = "2a"
        };

        viewModel.Details.Add(new QsoDetail { FieldName = "FD_CLASS", FieldValue = "1D" });
        viewModel.Details.Add(new QsoDetail { FieldName = "ARRL_SECT", FieldValue = "EMA" });
        viewModel.Details.Add(new QsoDetail { FieldName = "COUNTY", FieldValue = "PUL" });

        var updated = viewModel.BuildUpdatedQso(id);

        Assert.Contains(updated.Details, x => string.Equals(x.FieldName, "SECTION", StringComparison.OrdinalIgnoreCase) && x.FieldValue == "ENY");
        Assert.Contains(updated.Details, x => string.Equals(x.FieldName, "CLASS", StringComparison.OrdinalIgnoreCase) && x.FieldValue == "2A");
        Assert.DoesNotContain(updated.Details, x => string.Equals(x.FieldName, "ARRL_SECT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(updated.Details, x => string.Equals(x.FieldName, "FD_CLASS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(updated.Details, x => string.Equals(x.FieldName, "COUNTY", StringComparison.OrdinalIgnoreCase));
    }
}


