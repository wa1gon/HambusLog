using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using HamBusLog.Data;
using HamBusLog.Models;

namespace HamBusLog.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private AppConfiguration _appConfig = new();
    private string _selectedProfile = "default";
    private Color _backgroundColor = Color.Parse("#1F2937");
    private Color _foregroundColor = Color.Parse("#FFFFFF");
    private string _connectionString = "Data Source=hambuslog.db";
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;
    private string _statusMessage = string.Empty;
    private string _configFilePath = string.Empty;
    private List<string> _availableProfiles = new();

    public SettingsViewModel()
    {
        Load();
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                LoadProfile(value);
            }
        }
    }

    public List<string> AvailableProfiles
    {
        get => _availableProfiles;
        private set => SetProperty(ref _availableProfiles, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public Color ForegroundColor
    {
        get => _foregroundColor;
        set => SetProperty(ref _foregroundColor, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public string RigctldHost
    {
        get => _rigctldHost;
        set => SetProperty(ref _rigctldHost, value);
    }

    public int RigctldPort
    {
        get => _rigctldPort;
        set => SetProperty(ref _rigctldPort, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ConfigFilePath
    {
        get => _configFilePath;
        private set => SetProperty(ref _configFilePath, value);
    }

    public void Save()
    {
        try
        {
            var profile = new ConfigProfile
            {
                Name = _selectedProfile,
                BackgroundColor = BackgroundColor.ToString(),
                ForegroundColor = ForegroundColor.ToString(),
                ConnectionString = string.IsNullOrWhiteSpace(ConnectionString) ? "Data Source=hambuslog.db" : ConnectionString.Trim(),
                Rigctld = new RigctldConfiguration
                {
                    Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim(),
                    Port = RigctldPort <= 0 ? 4532 : RigctldPort
                }
            };

            _appConfig.Profiles[_selectedProfile] = profile;
            _appConfig.ActiveProfile = _selectedProfile;

            AppConfigurationStore.Save(_appConfig);
            StatusMessage = $"✓ Profile '{_selectedProfile}' saved at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Save failed: {ex.Message}";
        }
    }

    private void LoadProfile(string profileName)
    {
        if (_appConfig.Profiles.TryGetValue(profileName, out var profile))
        {
            try
            {
                BackgroundColor = Color.Parse(profile.BackgroundColor);
                ForegroundColor = Color.Parse(profile.ForegroundColor);
            }
            catch
            {
                BackgroundColor = Color.Parse("#1F2937");
                ForegroundColor = Color.Parse("#FFFFFF");
            }
            ConnectionString = profile.ConnectionString;
            RigctldHost = profile.Rigctld.Host;
            RigctldPort = profile.Rigctld.Port;
        }
    }

    private void Load()
    {
        _appConfig = AppConfigurationStore.Load();
        AvailableProfiles = _appConfig.Profiles.Keys.ToList();
        _selectedProfile = _appConfig.ActiveProfile;
        LoadProfile(_selectedProfile);
        ConfigFilePath = $"Config: {AppConfigurationStore.GetConfigFilePath()}";
    }
}

