using System;
using Avalonia.Media;
using HamBusLog.Data;
using HamBusLog.Models;

namespace HamBusLog.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private Color _backgroundColor = Color.Parse("#1F2937");
    private Color _foregroundColor = Color.Parse("#FFFFFF");
    private string _connectionString = "Data Source=hambuslog.db";
    private string _rigctldHost = "127.0.0.1";
    private int _rigctldPort = 4532;
    private string _statusMessage = string.Empty;
    private string _configFilePath = string.Empty;

    public SettingsViewModel()
    {
        Load();
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
            var configuration = new AppConfiguration
            {
                BackgroundColor = BackgroundColor.ToString(),
                ForegroundColor = ForegroundColor.ToString(),
                ConnectionString = string.IsNullOrWhiteSpace(ConnectionString) ? "Data Source=hambuslog.db" : ConnectionString.Trim(),
                Rigctld = new RigctldConfiguration
                {
                    Host = string.IsNullOrWhiteSpace(RigctldHost) ? "127.0.0.1" : RigctldHost.Trim(),
                    Port = RigctldPort <= 0 ? 4532 : RigctldPort
                }
            };

            AppConfigurationStore.Save(configuration);
            StatusMessage = $"✓ Saved at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Save failed: {ex.Message}";
        }
    }

    private void Load()
    {
        var configuration = AppConfigurationStore.Load();
        try
        {
            BackgroundColor = Color.Parse(configuration.BackgroundColor);
            ForegroundColor = Color.Parse(configuration.ForegroundColor);
        }
        catch
        {
            BackgroundColor = Color.Parse("#1F2937");
            ForegroundColor = Color.Parse("#FFFFFF");
        }
        ConnectionString = configuration.ConnectionString;
        RigctldHost = configuration.Rigctld.Host;
        RigctldPort = configuration.Rigctld.Port;
        ConfigFilePath = $"Config: {AppConfigurationStore.GetConfigFilePath()}";
    }
}

