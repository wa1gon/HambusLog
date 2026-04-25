namespace HamBusLog.ViewModels;

public sealed class AdifImportProgressViewModel : ViewModelBase
{
    private string _windowTitle = "Importing ADIF";
    private string _statusText = "Preparing import...";
    private string _fileName = string.Empty;
    private int _recordsRead;
    private int _savedChanges;
    private bool _isIndeterminate = true;
    private double _progressPercent;
    private bool _isCompleted;

    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public int RecordsRead
    {
        get => _recordsRead;
        set
        {
            if (SetProperty(ref _recordsRead, value))
                OnPropertyChanged(nameof(RecordsText));
        }
    }

    public int SavedChanges
    {
        get => _savedChanges;
        set
        {
            if (SetProperty(ref _savedChanges, value))
                OnPropertyChanged(nameof(SavedChangesText));
        }
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetProperty(ref _isIndeterminate, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public string RecordsText => $"Records read: {RecordsRead:N0}";
    public string SavedChangesText => SavedChanges > 0 ? $"Database changes: {SavedChanges:N0}" : string.Empty;

    public void Update(AdifImportProgress progress)
    {
        FileName = Path.GetFileName(progress.FilePath);
        StatusText = progress.StatusText;
        RecordsRead = progress.RecordsRead;
        SavedChanges = progress.SavedChanges;
        IsIndeterminate = progress.IsIndeterminate;
        ProgressPercent = Math.Clamp(progress.ProgressFraction * 100d, 0d, 100d);
        IsCompleted = progress.Stage == AdifImportStage.Completed;
    }
}

