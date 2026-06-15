namespace HamBusLog.ViewModels;

using HamBusLog.Models;
using HamBusLog.Services;

public sealed class LotwUploadViewModel : ViewModelBase
{
    private string _tqslPath;
    private string _downloadArgumentsTemplate;
    private string _stationCallsign;
    private string _passwordPlaintext;
    private bool _enabled;
    private bool _autoUploadOnLog;
    private bool _isUploading;
    private string _statusMessage = string.Empty;
    private string _uploadLog = string.Empty;
    private string _qsoRangeSummary = string.Empty;
    private DateTimeOffset? _uploadFrom;
    private DateTimeOffset? _uploadTo;

    public LotwUploadViewModel()
    {
        var config = AppConfigurationStore.Load();
        var lotw = config.Lotw;
        var profile = AppConfigurationStore.GetActiveProfile(config);

        _enabled = lotw.Enabled;
        _autoUploadOnLog = lotw.AutoUploadOnLog;
        _tqslPath = string.IsNullOrWhiteSpace(lotw.TqslPath) ? "tqsl" : lotw.TqslPath;
        _downloadArgumentsTemplate = string.IsNullOrWhiteSpace(lotw.DownloadArgumentsTemplate)
            ? "-d -c \"{callsign}\" -o \"{output}\""
            : lotw.DownloadArgumentsTemplate;
        _stationCallsign = string.IsNullOrWhiteSpace(lotw.StationCallsign)
            ? (profile.StationCallSign ?? string.Empty)
            : lotw.StationCallsign;
        _passwordPlaintext = WeakSecretProtector.Decrypt(lotw.PasswordCiphertext);

        _uploadFrom = new DateTimeOffset(DateTime.UtcNow.AddDays(-30).Date, TimeSpan.Zero);
        _uploadTo   = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        RefreshQsoRangeSummary();
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetProperty(ref _enabled, value))
                OnPropertyChanged(nameof(IsReadyToUpload));
        }
    }

    public bool AutoUploadOnLog
    {
        get => _autoUploadOnLog;
        set => SetProperty(ref _autoUploadOnLog, value);
    }

    public string TqslPath
    {
        get => _tqslPath;
        set => SetProperty(ref _tqslPath, value ?? string.Empty);
    }

    public string DownloadArgumentsTemplate
    {
        get => _downloadArgumentsTemplate;
        set => SetProperty(ref _downloadArgumentsTemplate, value ?? string.Empty);
    }

    public string StationCallsign
    {
        get => _stationCallsign;
        set
        {
            if (SetProperty(ref _stationCallsign, (value ?? string.Empty).ToUpperInvariant()))
                OnPropertyChanged(nameof(IsReadyToUpload));
        }
    }

    public string PasswordPlaintext
    {
        get => _passwordPlaintext;
        set => SetProperty(ref _passwordPlaintext, value ?? string.Empty);
    }

    public bool IsUploading
    {
        get => _isUploading;
        private set
        {
            if (SetProperty(ref _isUploading, value))
            {
                OnPropertyChanged(nameof(IsNotUploading));
                OnPropertyChanged(nameof(IsReadyToUpload));
            }
        }
    }

    public bool IsNotUploading => !_isUploading;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value ?? string.Empty);
    }

    public string UploadLog
    {
        get => _uploadLog;
        private set => SetProperty(ref _uploadLog, value ?? string.Empty);
    }

    public string QsoRangeSummary
    {
        get => _qsoRangeSummary;
        private set => SetProperty(ref _qsoRangeSummary, value ?? string.Empty);
    }

    public DateTimeOffset? UploadFrom
    {
        get => _uploadFrom;
        set
        {
            if (SetProperty(ref _uploadFrom, value))
                RefreshQsoRangeSummary();
        }
    }

    public DateTimeOffset? UploadTo
    {
        get => _uploadTo;
        set
        {
            if (SetProperty(ref _uploadTo, value))
                RefreshQsoRangeSummary();
        }
    }

    public bool IsReadyToUpload
        => _enabled && !_isUploading && !string.IsNullOrWhiteSpace(_stationCallsign) && !string.IsNullOrWhiteSpace(_tqslPath);

    public void SaveSettings()
    {
        var config = AppConfigurationStore.Load();
        config.Lotw.Enabled = _enabled;
        config.Lotw.AutoUploadOnLog = _autoUploadOnLog;
        config.Lotw.TqslPath = _tqslPath.Trim();
        config.Lotw.DownloadArgumentsTemplate = _downloadArgumentsTemplate.Trim();
        config.Lotw.StationCallsign = _stationCallsign.Trim().ToUpperInvariant();
        config.Lotw.PasswordCiphertext = string.IsNullOrWhiteSpace(_passwordPlaintext)
            ? string.Empty
            : WeakSecretProtector.Encrypt(_passwordPlaintext);
        AppConfigurationStore.Save(config);
    }

    public async Task UploadDateRangeAsync()
    {
        var from = (_uploadFrom ?? DateTimeOffset.UtcNow.AddDays(-30)).UtcDateTime.Date;
        var to   = (_uploadTo   ?? DateTimeOffset.UtcNow).UtcDateTime.Date.AddDays(1);

        var qsos = App.DbContext.Qsos
            .AsNoTracking()
            .Where(q => q.QsoDate >= from && q.QsoDate <= to)
            .Include(q => q.Details)
            .OrderBy(q => q.QsoDate)
            .ToList();

        if (qsos.Count == 0)
        {
            StatusMessage = "No QSOs found in the selected date range.";
            return;
        }

        await UploadInternalAsync(qsos);
    }

    public async Task UploadAllAsync()
    {
        var qsos = App.DbContext.Qsos
            .AsNoTracking()
            .Include(q => q.Details)
            .OrderBy(q => q.QsoDate)
            .ToList();

        if (qsos.Count == 0)
        {
            StatusMessage = "No QSOs in the logbook.";
            return;
        }

        await UploadInternalAsync(qsos);
    }

    public async Task DownloadAdifAsync(string outputPath)
    {
        SaveSettings();
        IsUploading = true;
        StatusMessage = $"Downloading LoTW ADIF to {Path.GetFileName(outputPath)}...";
        var log = new System.Text.StringBuilder();
        log.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] Starting download to {outputPath}...");

        var config = AppConfigurationStore.Load();
        var service = new LotwUploadService(config.Lotw, AppConfigurationStore.GetActiveProfile(config));

        var progress = new Progress<string>(msg =>
        {
            log.AppendLine(msg);
            UploadLog = log.ToString();
            StatusMessage = msg;
        });

        try
        {
            var result = await service.DownloadAdifAsync(outputPath, UploadFrom, UploadTo, progress, CancellationToken.None);
            log.AppendLine(result.Success ? $"SUCCESS: {result.Message}" : $"FAILED: {result.Message}");
            UploadLog = log.ToString();
            StatusMessage = result.Success ? $"✓ {result.Message}" : $"✗ {result.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task UploadInternalAsync(IReadOnlyList<Qso> qsos)
    {
        SaveSettings();
        IsUploading = true;
        StatusMessage = $"Uploading {qsos.Count} QSO(s)...";
        var log = new System.Text.StringBuilder();
        log.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] Starting upload of {qsos.Count} QSO(s)...");

        var config = AppConfigurationStore.Load();
        var service = new LotwUploadService(config.Lotw, AppConfigurationStore.GetActiveProfile(config));

        var progress = new Progress<string>(msg =>
        {
            log.AppendLine(msg);
            UploadLog = log.ToString();
            StatusMessage = msg;
        });

        try
        {
            var result = await service.UploadQsosAsync(qsos, progress, CancellationToken.None);
            log.AppendLine(result.Success ? $"SUCCESS: {result.Message}" : $"FAILED: {result.Message}");
            UploadLog = log.ToString();
            StatusMessage = result.Success ? $"✓ {result.Message}" : $"✗ {result.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private void RefreshQsoRangeSummary()
    {
        var from = (_uploadFrom ?? DateTimeOffset.UtcNow.AddDays(-30)).UtcDateTime.Date;
        var to   = (_uploadTo   ?? DateTimeOffset.UtcNow).UtcDateTime.Date.AddDays(1);

        try
        {
            var count = App.DbContext.Qsos.AsNoTracking()
                .Count(q => q.QsoDate >= from && q.QsoDate <= to);
            QsoRangeSummary = $"{count} QSO(s) in selected range";
        }
        catch
        {
            QsoRangeSummary = string.Empty;
        }
    }
}








