using HamBusLog.Data;
using HamBusLog.Models;
using HamBusLog.Services;
using HamBusLog.ViewModels;
using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.Tests;

[Collection("Config file tests")]
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

    [Fact]
    public void Load_NormalizesStoredLastContestKey_ToCanonicalContestKey()
    {
        var config = CreateConfiguration();
        config.Profiles["default"].LastContestKey = ContestCatalog.ArrlFieldDayAdifId;
        SaveConfiguration(config);

        var loaded = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(loaded);

        Assert.Equal(ContestCatalog.ArrlFieldDayKey, profile.LastContestKey);
    }

    [Fact]
    public void LogTypeSelectionService_PersistsSelectedContest_WhenItChanges()
    {
        var config = CreateConfiguration();
        config.Profiles["default"].LastContestKey = ContestCatalog.NormalKey;
        SaveConfiguration(config);

        var service = new LogTypeSelectionService();
        service.SetSelectedContestKey(ContestCatalog.ArrlFieldDayAdifId);

        var saved = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(saved);

        Assert.Equal(ContestCatalog.ArrlFieldDayKey, profile.LastContestKey);
    }

    [Fact]
    public void Load_NormalContest_RemovesLegacyExtraRequiredFields()
    {
        var config = CreateConfiguration();
        config.Contests =
        [
            new ContestDefinitionConfig
            {
                Key = ContestCatalog.NormalKey,
                DisplayName = "Normal",
                AdifContestId = ContestCatalog.NormalKey,
                ExchangeType = "normal",
                RequiredFields =
                [
                    new ContestFieldRequirementConfig { Key = ContestFieldKeys.RstSent, Label = "RST Sent" },
                    new ContestFieldRequirementConfig { Key = ContestFieldKeys.RstRecv, Label = "RST Rec" },
                    new ContestFieldRequirementConfig { Key = ContestFieldKeys.Country, Label = "Country" },
                    new ContestFieldRequirementConfig { Key = ContestFieldKeys.Name, Label = "Name", DetailFieldName = "Name" },
                    new ContestFieldRequirementConfig { Key = ContestFieldKeys.State, Label = "State" },
                    new ContestFieldRequirementConfig { Key = ContestFieldKeys.County, Label = "County", DetailFieldName = "County" }
                ]
            }
        ];
        SaveConfiguration(config);

        var loaded = AppConfigurationStore.Load();
        var normal = loaded.Contests.Single(x => string.Equals(x.Key, ContestCatalog.NormalKey, StringComparison.OrdinalIgnoreCase));

        Assert.Empty(normal.RequiredFields);
    }

    [Fact]
    public void LogInputViewModel_UsesGlobalNormalSelection_WhenProfileStillHasFieldDayStored()
    {
        var config = CreateConfiguration();
        config.Profiles["default"].LastContestKey = ContestCatalog.ArrlFieldDayKey;
        SaveConfiguration(config);

        App.LogTypeSelectionService.SetSelectedContestKey(ContestCatalog.NormalKey);

        using var vm = new LogInputViewModel();

        Assert.Equal(ContestType.Normal, vm.SelectedContestType);
        Assert.False(vm.IsFieldDay);
        Assert.True(vm.IsNormalContest);
    }

    [Fact]
    public void Save_ClampsAndPersistsAppFontSize()
    {
        SaveConfiguration(CreateConfiguration());

        using var viewModel = new ConfigurationViewModel();
        viewModel.AppFontSize = 42;

        viewModel.Save();

        var saved = AppConfigurationStore.Load();
        var profile = saved.Profiles["default"];
        Assert.Equal(24, profile.AppFontSize);

        using var reloaded = new ConfigurationViewModel();
        Assert.Equal(24, reloaded.AppFontSize);
    }

    [Fact]
    public void FontSizePreset_UpdatesSize_AndCustomValueSetsCustomPreset()
    {
        SaveConfiguration(CreateConfiguration());

        using var viewModel = new ConfigurationViewModel();
        viewModel.SelectedFontSizePreset = "Large (14 pt)";

        Assert.Equal(14, viewModel.AppFontSize);

        viewModel.AppFontSize = 13;

        Assert.Equal("Custom", viewModel.SelectedFontSizePreset);
    }

    [Fact]
    public void ResetColorsToDefaults_RestoresReadablePalette()
    {
        SaveConfiguration(CreateConfiguration());

        using var viewModel = new ConfigurationViewModel();
        viewModel.BackgroundColor = Avalonia.Media.Color.Parse("#FFFFFF");
        viewModel.ForegroundColor = Avalonia.Media.Color.Parse("#000000");
        viewModel.ButtonDangerColor = Avalonia.Media.Color.Parse("#FF00FF");

        viewModel.ResetColorsToDefaults();

        Assert.Equal(Avalonia.Media.Color.Parse("#0F172A"), viewModel.BackgroundColor);
        Assert.Equal(Avalonia.Media.Color.Parse("#E5E7EB"), viewModel.ForegroundColor);
        Assert.Equal(Avalonia.Media.Color.Parse("#B91C1C"), viewModel.ButtonDangerColor);
    }

    [Fact]
    public void Load_BackfillsArrlFdRequiredFields_WhenContestEntryIsMissingThem()
    {
        File.WriteAllText(_configPath,
            """
            {
              "ActiveProfile": "default",
              "Profiles": {
                "default": {
                  "Name": "default",
                  "ConnectionString": "Data Source=hambuslog.db"
                }
              },
              "Contests": [
                {
                  "Key": "ARRL-FD",
                  "DisplayName": "ARRL Field Day",
                  "AdifContestId": "ARRL-FIELD-DAY",
                  "ExchangeType": "fieldday",
                  "RequiredFields": []
                }
              ]
            }
            """);

        var loaded = AppConfigurationStore.Load();
        var fieldDay = loaded.Contests.Single(x => string.Equals(x.Key, "ARRL-FD", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(fieldDay.RequiredFields, x => string.Equals(x.Key, "fd_section", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fieldDay.RequiredFields, x => string.Equals(x.Key, "fd_class", StringComparison.OrdinalIgnoreCase));

        Assert.True(AppConfigurationStore.ConsumeContestRepairNotice());
        Assert.False(AppConfigurationStore.ConsumeContestRepairNotice());
    }

    [Fact]
    public void Load_BackfillsFieldDayRequiredFields_ForCustomFdKey()
    {
        File.WriteAllText(_configPath,
            """
            {
              "ActiveProfile": "default",
              "Profiles": {
                "default": {
                  "Name": "default",
                  "ConnectionString": "Data Source=hambuslog.db"
                }
              },
              "Contests": [
                {
                  "Key": "FD",
                  "DisplayName": "FD",
                  "AdifContestId": "FD",
                  "ExchangeType": "fieldday",
                  "RequiredFields": []
                }
              ]
            }
            """);

        var loaded = AppConfigurationStore.Load();
        var fieldDay = loaded.Contests.Single(x => string.Equals(x.Key, "FD", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(fieldDay.RequiredFields, x => string.Equals(x.Key, "fd_section", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fieldDay.RequiredFields, x => string.Equals(x.Key, "fd_class", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContestCatalog_ReturnsIndependentFieldDayCopies()
    {
        File.WriteAllText(_configPath,
            """
            {
              "ActiveProfile": "default",
              "Profiles": {
                "default": {
                  "Name": "default",
                  "ConnectionString": "Data Source=hambuslog.db"
                }
              },
              "Contests": [
                {
                  "Key": "ARRL-FD",
                  "DisplayName": "ARRL Field Day",
                  "AdifContestId": "ARRL-FIELD-DAY",
                  "ExchangeType": "fieldday",
                  "RequiredFields": []
                }
              ]
            }
            """);

        var first = ContestCatalog.Get(ContestType.ArrlFieldDay);
        var second = ContestCatalog.Get(ContestType.ArrlFieldDay);

        Assert.NotSame(first, second);
        Assert.NotSame(first.RequiredFields, second.RequiredFields);

        var mutableFirstFields = Assert.IsType<List<ContestFieldRequirement>>(first.RequiredFields);
        mutableFirstFields.Add(new ContestFieldRequirement("test", "Test"));

        Assert.DoesNotContain(second.RequiredFields, x => string.Equals(x.Key, "test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Load_NormalizesArrlFdExchangeTypeAndRequiredFields_WhenExchangeTypeWasCorrupted()
    {
        File.WriteAllText(_configPath,
            """
            {
              "ActiveProfile": "default",
              "Profiles": {
                "default": {
                  "Name": "default",
                  "ConnectionString": "Data Source=hambuslog.db"
                }
              },
              "Contests": [
                {
                  "Key": "ARRL-FD",
                  "DisplayName": "ARRL Field Day",
                  "AdifContestId": "ARRL-FD",
                  "ExchangeType": "normal",
                  "RequiredFields": []
                }
              ]
            }
            """);

        var loaded = AppConfigurationStore.Load();
        var fieldDay = loaded.Contests.Single(x => string.Equals(x.Key, "ARRL-FD", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("fieldday", fieldDay.ExchangeType);
        Assert.Contains(fieldDay.RequiredFields, x => string.Equals(x.Key, "fd_section", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fieldDay.RequiredFields, x => string.Equals(x.Key, "fd_class", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LogInputViewModel_ShowsFieldDayFields_WhenArrlFdExchangeTypeWasCorrupted()
    {
        File.WriteAllText(_configPath,
            """
            {
              "ActiveProfile": "default",
              "Profiles": {
                "default": {
                  "Name": "default",
                  "ConnectionString": "Data Source=hambuslog.db"
                }
              },
              "Contests": [
                {
                  "Key": "ARRL-FD",
                  "DisplayName": "ARRL Field Day",
                  "AdifContestId": "ARRL-FD",
                  "ExchangeType": "normal",
                  "RequiredFields": []
                }
              ]
            }
            """);

        var vm = new LogInputViewModel();
        vm.SelectedContestType = ContestType.ArrlFieldDay;

        Assert.True(vm.IsFieldDay);
        Assert.True(vm.ShowFieldDaySection);
        Assert.True(vm.ShowFieldDayClass);
    }

    [Fact]
    public void LogInputViewModel_DuplicateWarning_WhenCallAlreadyLoggedForSameContestBandAndMode()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-check.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "K1ABC",
                ContestId = "NORMAL",
                QsoDate = DateTime.UtcNow,
                Band = "20M",
                Mode = "SSB"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                SelectedContestType = ContestType.Normal,
                InputCall = "k1abc",
                InputBand = "20m",
                InputMode = "ssb"
            };

            var hasDuplicateWarning = vm.TryGetDuplicateCallWarning(out var warning);

            Assert.True(hasDuplicateWarning);
            Assert.Contains("Possible duplicate:", warning, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("K1ABC", warning, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_DuplicateWarning_WhenContestIdDoesNotMatchButBandModeDo()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-check-contest-mismatch.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "K5XYZ",
                ContestId = string.Empty,
                QsoDate = DateTime.UtcNow,
                Band = "20M",
                Mode = "SSB"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                SelectedContestType = ContestType.Normal,
                InputCall = "k5xyz",
                InputBand = "20m",
                InputMode = "ssb"
            };

            var hasDuplicateWarning = vm.TryGetDuplicateCallWarning(out var warning);

            Assert.True(hasDuplicateWarning);
            Assert.Contains("Possible duplicate:", warning, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("K5XYZ", warning, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_TryBuildQso_BlocksDuplicate_WhenCallBandAndModeMatch()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-block-exact.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "K1ABC",
                ContestId = "ARRL-FD",
                QsoDate = DateTime.UtcNow,
                Band = "20M",
                Mode = "SSB"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                InputCall = "k1abc",
                InputDate = "20260613",
                InputTimeOn = "1930",
                InputBand = "20m",
                InputMode = "ssb"
            };

            var qso = vm.TryBuildQso(out var error);

            Assert.Null(qso);
            Assert.Contains("Duplicate QSO not allowed", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_TryBuildQso_BlocksDuplicate_WhenBothModesAreDigital()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-block-digital.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "W1DIG",
                ContestId = "NORMAL",
                QsoDate = DateTime.UtcNow,
                Band = "40M",
                Mode = "FT8"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                InputCall = "w1dig",
                InputDate = "20260613",
                InputTimeOn = "1935",
                InputBand = "40m",
                InputMode = "rtty"
            };

            var qso = vm.TryBuildQso(out var error);

            Assert.Null(qso);
            Assert.Contains("Duplicate QSO not allowed", error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DIGITAL", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_TryBuildQso_BlocksDuplicate_WhenModesAreInPhoneFamily()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-block-phone.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "N0PHN",
                ContestId = "NORMAL",
                QsoDate = DateTime.UtcNow,
                Band = "20M",
                Mode = "FM"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                InputCall = "n0phn",
                InputDate = "20260613",
                InputTimeOn = "1940",
                InputBand = "20m",
                InputMode = "ssb"
            };

            var qso = vm.TryBuildQso(out var error);

            Assert.Null(qso);
            Assert.Contains("Duplicate QSO not allowed", error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PHONE", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_DuplicateWarning_WhenModesAreInPhoneFamily()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-warning-phone.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "W1PHN",
                ContestId = "NORMAL",
                QsoDate = DateTime.UtcNow,
                Band = "40M",
                Mode = "AM"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                InputCall = "w1phn",
                InputBand = "40m",
                InputMode = "lsb"
            };

            var hasDuplicateWarning = vm.TryGetDuplicateCallWarning(out var warning);

            Assert.True(hasDuplicateWarning);
            Assert.Contains("Possible duplicate:", warning, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PHONE", warning, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_TryBuildQso_BlocksDuplicate_WhenBandFormattingDiffers()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-block-band-format.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "N1FMT",
                ContestId = "NORMAL",
                QsoDate = DateTime.UtcNow,
                Band = "20 M",
                Mode = "FM"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                InputCall = "n1fmt",
                InputDate = "20260613",
                InputTimeOn = "1945",
                InputBand = "20m",
                InputMode = "SSB"
            };

            var qso = vm.TryBuildQso(out var error);

            Assert.Null(qso);
            Assert.Contains("Duplicate QSO not allowed", error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PHONE", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void LogInputViewModel_TryBuildQso_BlocksDuplicate_WhenBandIsBlankButFrequencyMatches()
    {
        var dbPath = Path.Combine(_tempDirectory, "dup-block-blank-band-freq.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            App.DbContext.Qsos.Add(new Qso
            {
                Id = Guid.NewGuid(),
                Call = "KB1ETC",
                ContestId = "NORMAL",
                QsoDate = DateTime.UtcNow,
                Band = string.Empty,
                Freq = 145.000m,
                Mode = "FM"
            });
            App.DbContext.SaveChanges();

            var vm = new LogInputViewModel
            {
                InputCall = "kb1etc",
                InputDate = "20260613",
                InputTimeOn = "2359",
                InputBand = string.Empty,
                InputFreq = "145.000",
                InputMode = "FM"
            };

            var qso = vm.TryBuildQso(out var error);

            Assert.Null(qso);
            Assert.Contains("Duplicate QSO not allowed", error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("2M", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
    }

    [Fact]
    public void ArrlFdProgressViewModel_TracksDx_WhenFdDetailsExistWithoutContestId()
    {
        var dbPath = Path.Combine(_tempDirectory, "fd-progress.db");
        var connectionString = $"Data Source={dbPath}";

        var original = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(original);
        var originalConnectionString = profile.ConnectionString;
        profile.ConnectionString = connectionString;
        AppConfigurationStore.Save(original);

        try
        {
            Assert.True(App.ReinitializeDbContext(connectionString, out _));

            var qsoId = Guid.NewGuid();
            App.DbContext.Qsos.Add(new Qso
            {
                Id = qsoId,
                Call = "K1DX",
                ContestId = string.Empty,
                State = "DX",
                Country = "SPAIN",
                QsoDate = DateTime.UtcNow,
                Band = "20M",
                Mode = "SSB"
            });
            App.DbContext.QsoDetails.AddRange(
                new QsoDetail { QsoId = qsoId, FieldName = "Section", FieldValue = "DX" },
                new QsoDetail { QsoId = qsoId, FieldName = "Class", FieldValue = "1D" });
            App.DbContext.SaveChanges();

            var vm = new ArrlFdProgressViewModel();
            vm.Refresh();

            Assert.Contains(vm.SectionRows, x => string.Equals(x.Code, "DX", StringComparison.OrdinalIgnoreCase) && x.IsWorked);
            Assert.Equal("DX: Worked", vm.DxSummary);
        }
        finally
        {
            profile.ConnectionString = originalConnectionString;
            AppConfigurationStore.Save(original);
            App.ReinitializeDbContext(originalConnectionString, out _);
        }
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



