using System.Threading.Tasks;

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
}
