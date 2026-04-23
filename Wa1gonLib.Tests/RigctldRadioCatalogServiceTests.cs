namespace Wa1gonLib.Tests;

public class RigctldRadioCatalogServiceTests
{
    [Fact]
    public async Task GetAllRadiosAsync_ReturnsEmptyWhenExecutionIsDisabled()
    {
        var service = new RigctldRadioCatalogService();

        var radios = await service.GetAllRadiosAsync();

        Assert.Empty(radios);
    }

    [Fact]
    public void ParseRigList_ParsesSampleOutput()
    {
        const string sample = """
             1  Hamlib                 Dummy                   20221128.0      Stable      RIG_MODEL_DUMMY
             2  Hamlib                 NET rigctl              20230328.0      Stable      RIG_MODEL_NETRIGCTL
             4  FLRig                  FLRig                   20221109.0      Stable      RIG_MODEL_FLRIG
            """;

        var radios = RigctldRadioCatalogService.ParseRigList(sample);

        Assert.Equal(3, radios.Count);
        Assert.Equal(1, radios[0].RigNum);
        Assert.Equal("Hamlib", radios[0].Mfg);
        Assert.Equal("Dummy", radios[0].Model);
        Assert.Equal("20221128.0", radios[0].Version);
        Assert.Equal("Stable", radios[0].Status);
        Assert.Equal("RIG_MODEL_DUMMY", radios[0].Macro);
        Assert.Equal("NET rigctl", radios[1].Model);
    }

    [Fact]
    public void GetAllRadiosFromText_ParsesSampleOutput()
    {
        const string sample = """
             1  Hamlib                 Dummy                   20221128.0      Stable      RIG_MODEL_DUMMY
             2  Hamlib                 NET rigctl              20230328.0      Stable      RIG_MODEL_NETRIGCTL
            """;

        var service = new RigctldRadioCatalogService();
        var radios = service.GetAllRadiosFromText(sample);

        Assert.Equal(2, radios.Count);
        Assert.Equal(2, radios[1].RigNum);
        Assert.Equal("NET rigctl", radios[1].Model);
    }

    [Fact]
    public void ParseRigList_IgnoresMalformedAndNonDataLines()
    {
        const string sample = """
            Rig list header
            foo bar baz
            1  Hamlib                 Dummy                   20221128.0      Stable      RIG_MODEL_DUMMY
            999 this-line-will-be-ignored
            """;

        var radios = RigctldRadioCatalogService.ParseRigList(sample);

        Assert.Single(radios);
        Assert.Equal(1, radios[0].RigNum);
    }

    [Fact]
    public void FilterByModel_MatchesModelCaseInsensitively()
    {
        var entries = new[]
        {
            new RigCatalogEntry { RigNum = 1, Mfg = "Hamlib", Model = "Dummy", Version = "1", Status = "Stable", Macro = "A" },
            new RigCatalogEntry { RigNum = 2, Mfg = "Hamlib", Model = "NET rigctl", Version = "1", Status = "Stable", Macro = "B" },
            new RigCatalogEntry { RigNum = 3, Mfg = "FLRig", Model = "FLRig", Version = "1", Status = "Stable", Macro = "C" }
        };

        var filtered = RigctldRadioCatalogService.FilterByModel(entries, "rig");

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, x => x.Model == "NET rigctl");
        Assert.Contains(filtered, x => x.Model == "FLRig");
    }

    [Fact]
    public void CreateRigctldCommandLine_UsesRigNumberHostAndPort()
    {
        var entry = new RigCatalogEntry
        {
            RigNum = 123,
            Mfg = "Yaesu",
            Model = "FT-710",
            Version = "1",
            Status = "Stable",
            Macro = "RIG_MODEL_FT710"
        };

        var command = RigctldRadioCatalogService.CreateRigctldCommandLine(entry, "0.0.0.0", 4600);

        Assert.Equal("rigctld -m 123 -T 0.0.0.0 -t 4600", command);
    }

    [Fact]
    public void CreateRigctldCommandLine_AppendsSerialPortWhenProvided()
    {
        var entry = new RigCatalogEntry
        {
            RigNum = 123,
            Mfg = "Yaesu",
            Model = "FT-710",
            Version = "1",
            Status = "Stable",
            Macro = "RIG_MODEL_FT710"
        };

        var command = RigctldRadioCatalogService.CreateRigctldCommandLine(entry, "127.0.0.1", 4532, "/dev/serial/by-id/usb-FT710 CAT");

        Assert.Equal("rigctld -m 123 -T 127.0.0.1 -t 4532 -r \"/dev/serial/by-id/usb-FT710 CAT\"", command);
    }
}
