using HamBusLog.Data;
using HamBusLog.Models;
using HamBusLog.ViewModels;
using Xunit;

namespace HamBusLog.Tests;

public sealed class ConfigurationViewModelRigRadioTests : IDisposable
{
    private static readonly object ConfigLock = new();

    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "hambuslog-tests", Guid.NewGuid().ToString("N"));
    private readonly string _configPath = AppConfigurationStore.GetConfigFilePath();
    private readonly bool _hadOriginalConfig;
    private readonly string? _originalConfigContent;

    public ConfigurationViewModelRigRadioTests()
    {
        Monitor.Enter(ConfigLock);
        Directory.CreateDirectory(_tempDirectory);

        if (File.Exists(_configPath))
        {
            _hadOriginalConfig = true;
            _originalConfigContent = File.ReadAllText(_configPath);
        }

        var configDirectory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(configDirectory))
            Directory.CreateDirectory(configDirectory);

        App.RigCatalogStore.Clear();
    }

    [Fact]
    public void CommitSelectedRigRadioEdits_PersistsRenamedRadio()
    {
        SaveConfiguration(CreateConfiguration());

        using var viewModel = new ConfigurationViewModel();
        viewModel.AddRigRadio();
        viewModel.RigctldRadioName = "FT-710";
        viewModel.RigctldHost = "192.168.1.25";
        viewModel.ResourcePath = "/dev/ttyUSB0";

        var committed = viewModel.CommitSelectedRigRadioEdits();

        Assert.True(committed);
        Assert.Equal("FT-710", viewModel.SelectedRigRadioName);
        Assert.Contains(viewModel.AvailableRigRadioOptions, x => x.RadioId == 2 && x.RadioName == "FT-710");

        var savedConfig = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(savedConfig);
        var savedRadio = rigctld.Radios.Single(x => x.RadioId == 2);
        Assert.Equal("FT-710", savedRadio.RadioName);
        Assert.Equal("192.168.1.25", savedRadio.Host);
        Assert.Equal("/dev/ttyUSB0", savedRadio.SerialPortName);
    }

    [Fact]
    public void Constructor_LoadsConfiguredRiglistIntoRigCatalog()
    {
        var riglistPath = WriteRiglistFile();
        SaveConfiguration(CreateConfiguration(riglistPath));

        using var viewModel = new ConfigurationViewModel();

        Assert.Equal(riglistPath, viewModel.RiglistFilePath);
        Assert.Equal(riglistPath, viewModel.RigCatalog.FilePath);
        Assert.NotEmpty(viewModel.RigCatalog.FilteredEntries);
        Assert.Contains(viewModel.RigCatalog.FilteredEntries, x => x.RigNum == 3070 && x.Model == "FT-710");
    }

    [Fact]
    public void Constructor_NormalizesDatabaseFolderPathWhenItContainsFileName()
    {
        var mistakenFolderValue = Path.Combine(_tempDirectory, "station.db");
        var config = CreateConfiguration();
        config.Profiles["default"].DatabaseFolderPath = mistakenFolderValue;
        config.Profiles["default"].DatabaseFileName = "hambuslog.db";
        config.Profiles["default"].DatabaseFilePath = string.Empty;
        SaveConfiguration(config);

        using var viewModel = new ConfigurationViewModel();

        var expectedFolder = Path.GetFullPath(_tempDirectory);
        var expectedPath = Path.Combine(expectedFolder, "station.db");
        Assert.Equal(expectedFolder, viewModel.DatabaseFolderPath);
        Assert.Equal("station.db", viewModel.DatabaseFileName);
        Assert.Equal(expectedPath, viewModel.DatabaseFilePath);
    }

    [Fact]
    public void Save_NormalizesDatabaseFolderPathWhenItContainsFileName()
    {
        SaveConfiguration(CreateConfiguration());

        using var viewModel = new ConfigurationViewModel();
        viewModel.DatabaseFolderPath = Path.Combine(_tempDirectory, "portable.sqlite3");
        viewModel.DatabaseFileName = "hambuslog.db";
        viewModel.ConnectionString = "Data Source=hambuslog.db";

        viewModel.Save();

        var saved = AppConfigurationStore.Load();
        var profile = saved.Profiles["default"];
        var expectedFolder = Path.GetFullPath(_tempDirectory);
        var expectedPath = Path.Combine(expectedFolder, "portable.sqlite3");
        Assert.Equal(expectedFolder, profile.DatabaseFolderPath);
        Assert.Equal("portable.sqlite3", profile.DatabaseFileName);
        Assert.Equal(expectedPath, profile.DatabaseFilePath);
        Assert.Equal($"Data Source={expectedPath}", profile.ConnectionString);
    }

    public void Dispose()
    {
        try
        {
            RestoreOriginalConfiguration();
            App.RigCatalogStore.Clear();
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        finally
        {
            Monitor.Exit(ConfigLock);
        }
    }

    private void SaveConfiguration(AppConfiguration configuration)
    {
        AppConfigurationStore.Save(configuration);
        App.RigCatalogStore.Clear();
    }

    private AppConfiguration CreateConfiguration(string riglistPath = "") => new()
    {
        ActiveProfile = "default",
        Profiles = new Dictionary<string, ConfigProfile>
        {
            ["default"] = new()
            {
                Name = "default",
                ConnectionString = "Data Source=hambuslog.db"
            }
        },
        Rigctld = new RigctldConfiguration
        {
            ActiveRadioName = "Base Radio",
            ActiveRadioNames = ["Base Radio"],
            ActiveRigNum = 3070,
            RiglistFilePath = riglistPath,
            Radios =
            [
                new RigRadioConfig
                {
                    RadioId = 1,
                    RadioName = "Base Radio",
                    Host = "127.0.0.1",
                    Port = 4532,
                    SerialPortName = string.Empty,
                    IsActive = true
                }
            ]
        }
    };

    private string WriteRiglistFile()
    {
        var path = Path.Combine(_tempDirectory, "riglist.txt");
        File.WriteAllText(path,
            "3070  Yaesu  FT-710  0.1  Stable  ft710\n" +
            "1234  Icom  IC-7300  1.0  Stable  ic7300\n");
        return path;
    }

    private void RestoreOriginalConfiguration()
    {
        if (_hadOriginalConfig)
        {
            File.WriteAllText(_configPath, _originalConfigContent ?? string.Empty);
            return;
        }

        if (File.Exists(_configPath))
            File.Delete(_configPath);
    }
}



