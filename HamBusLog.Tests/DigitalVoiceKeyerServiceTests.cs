using HamBusLog.Data;
using HamBusLog.Models;
using HamBusLog.Services;
using Xunit;

namespace HamBusLog.Tests;

[Collection("Config file tests")]
public sealed class DigitalVoiceKeyerServiceTests : IDisposable
{
    private static readonly object ConfigLock = new();

    private readonly string _configPath = AppConfigurationStore.GetConfigFilePath();
    private readonly bool _hadOriginalConfig;
    private readonly string? _originalConfigContent;

    public DigitalVoiceKeyerServiceTests()
    {
        Monitor.Enter(ConfigLock);

        if (File.Exists(_configPath))
        {
            _hadOriginalConfig = true;
            _originalConfigContent = File.ReadAllText(_configPath);
        }

        var configDirectory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(configDirectory))
            Directory.CreateDirectory(configDirectory);

        AppConfigurationStore.Save(new AppConfiguration());
    }

    [Fact]
    public void GetRecordsForLogType_ReturnsTenSlots_ForNewBank()
    {
        var service = new DigitalVoiceKeyerService();

        var records = service.GetRecordsForLogType("ARRL-FD");

        Assert.Equal(10, records.Count);
        for (var slot = 1; slot <= 10; slot++)
        {
            var record = Assert.Single(records, x => x.SlotNumber == slot);
            Assert.False(string.IsNullOrWhiteSpace(record.Label));
        }
    }

    [Fact]
    public void SaveRecordsForLogType_PersistsDistinctBanks_PerLogType()
    {
        var service = new DigitalVoiceKeyerService();

        var normalRecords = Enumerable.Range(1, 10)
            .Select(slot => new DigitalVoiceKeyerRecord
            {
                SlotNumber = slot,
                Label = $"Normal {slot}",
                Message = $"CQ NORMAL {slot}"
            })
            .ToList();

        var fieldDayRecords = Enumerable.Range(1, 10)
            .Select(slot => new DigitalVoiceKeyerRecord
            {
                SlotNumber = slot,
                Label = $"FD {slot}",
                Message = $"CQ FD {slot}"
            })
            .ToList();

        Assert.Equal("CQ NORMAL 1", normalRecords[0].Message);
        Assert.Equal("CQ FD 1", fieldDayRecords[0].Message);

        service.SaveRecordsForLogType("NORMAL", normalRecords);
        service.SaveRecordsForLogType("ARRL-FD", fieldDayRecords);

        var loadedNormal = service.GetRecordsForLogType("NORMAL");
        var loadedFieldDay = service.GetRecordsForLogType("ARRL-FD");

        Assert.Equal("CQ NORMAL 1", loadedNormal.Single(x => x.SlotNumber == 1).Message);
        Assert.Equal("CQ FD 1", loadedFieldDay.Single(x => x.SlotNumber == 1).Message);
        Assert.NotEqual(
            loadedNormal.Single(x => x.SlotNumber == 1).Message,
            loadedFieldDay.Single(x => x.SlotNumber == 1).Message);
    }

    [Fact]
    public void AppConfigurationStore_PreservesDigitalVoiceKeyerMessage()
    {
        var config = AppConfigurationStore.Load();
        config.DigitalVoiceKeyer.Banks["NORMAL"] = new DigitalVoiceKeyerBankConfig
        {
            LogTypeKey = "NORMAL",
            Records =
            [
                new DigitalVoiceKeyerRecordConfig { SlotNumber = 1, Label = "Normal 1", Message = "CQ TEST" }
            ]
        };

        AppConfigurationStore.Save(config);

        var reloaded = AppConfigurationStore.Load();
        var record = reloaded.DigitalVoiceKeyer.Banks["NORMAL"].Records.Single(x => x.SlotNumber == 1);
        Assert.Equal("CQ TEST", record.Message);
    }

    [Fact]
    public void SetPreferredPlaybackDevice_PersistsInConfiguration()
    {
        var service = new DigitalVoiceKeyerService();

        service.SetPreferredPlaybackDevice("  alsa_output.pci-0000_00_1f.3.analog-stereo  ");

        var reloaded = AppConfigurationStore.Load();
        Assert.Equal("alsa_output.pci-0000_00_1f.3.analog-stereo", reloaded.DigitalVoiceKeyer.OutputDevice);
        Assert.Equal("alsa_output.pci-0000_00_1f.3.analog-stereo", service.GetPreferredPlaybackDevice());
    }

    [Fact]
    public void SetPreferredPlaybackDevice_SystemDefaultPersistsAsEmptyRawValue()
    {
        var service = new DigitalVoiceKeyerService();

        service.SetPreferredPlaybackDevice(DigitalVoiceKeyerService.SystemDefaultPlaybackDevice);

        var reloaded = AppConfigurationStore.Load();
        Assert.Equal(string.Empty, reloaded.DigitalVoiceKeyer.OutputDevice);
        Assert.Equal(string.Empty, service.GetPreferredPlaybackDevice());
    }

    [Fact]
    public void GetPreferredPlaybackDevice_SystemDefaultReturnsEmptyString()
    {
        var service = new DigitalVoiceKeyerService();

        service.SetPreferredPlaybackDevice(DigitalVoiceKeyerService.SystemDefaultPlaybackDevice);

        Assert.Equal(string.Empty, service.GetPreferredPlaybackDevice());
    }

    [Fact]
    public void SaveRecordsForLogType_PersistsRepeatDelaySeconds()
    {
        var service = new DigitalVoiceKeyerService();
        var records = Enumerable.Range(1, 10)
            .Select(slot => new DigitalVoiceKeyerRecord
            {
                SlotNumber = slot,
                Label = $"Normal {slot}",
                RepeatDelaySeconds = slot == 3 ? 7 : 0
            })
            .ToList();

        service.SaveRecordsForLogType("NORMAL", records);

        var loaded = service.GetRecordsForLogType("NORMAL");
        Assert.Equal(7, loaded.Single(x => x.SlotNumber == 3).RepeatDelaySeconds);
    }

    [Fact]
    public void SetCompactViewEnabled_PersistsInConfiguration()
    {
        var service = new DigitalVoiceKeyerService();

        service.SetCompactViewEnabled(true);

        var reloaded = AppConfigurationStore.Load();
        Assert.True(reloaded.DigitalVoiceKeyer.CompactView);
        Assert.True(service.GetCompactViewEnabled());
    }

    [Fact]
    public void BuildPlaybackDeviceDisplayName_UsesFriendlyDescriptionWhenAvailable()
    {
        var display = DigitalVoiceKeyerService.BuildPlaybackDeviceDisplayName(
            "alsa_output.pci-0000_00_1f.3.analog-stereo",
            "Built-in Audio Analog Stereo");

        Assert.Equal(
            "Built-in Audio Analog Stereo (alsa_output.pci-0000_00_1f.3.analog-stereo)",
            display);
    }

    [Fact]
    public void BuildPlaybackDeviceDisplayName_HumanizesRawDeviceNameWhenNoDescriptionExists()
    {
        var display = DigitalVoiceKeyerService.BuildPlaybackDeviceDisplayName("alsa_output.usb-Logitech_USB_Headset-00.analog-stereo");

        Assert.Contains("Usb Logitech USB Headset", display, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        try
        {
            if (_hadOriginalConfig)
            {
                File.WriteAllText(_configPath, _originalConfigContent ?? string.Empty);
            }
            else if (File.Exists(_configPath))
            {
                File.Delete(_configPath);
            }
        }
        finally
        {
            Monitor.Exit(ConfigLock);
        }
    }
}










