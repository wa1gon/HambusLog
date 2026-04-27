namespace HamBusLog.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    public MenuNode[] MenuItems { get; } =
    [
        new MenuNode("Grid"),
        new MenuNode("Add New Contact"),
        new MenuNode("File", true,
            new MenuNode("Open/Reopen Grid"),
            new MenuNode("Import ADIF"),
            new MenuNode("Export ADIF"),
            new MenuNode("Remove Dups"),
            new MenuNode("Watch List")),
        new MenuNode("Edit"),
        new MenuNode("Configuration"),
        new MenuNode("Callbook"),
        new MenuNode("List"),
        new MenuNode("Search"),
        new MenuNode("Awards"),
        new MenuNode("eLogs"),
        new MenuNode("RecCall"),
        new MenuNode("Net View"),
        new MenuNode("Help")
    ];

    private MenuNode? _selectedMenuItem;
    private readonly RigCatalogStore _rigCatalogStore;
    private ObservableCollection<ActiveRadioOption> _availableRadios = [];
    private ActiveRadioOption? _selectedActiveRadio;

    public MainWindowViewModel()
    {
        _rigCatalogStore = App.RigCatalogStore;
        _rigCatalogStore.PropertyChanged += OnRigCatalogStorePropertyChanged;
        RefreshActiveRadioOptions();
        SelectedMenuItem = MenuItems[0];
    }

    public MenuNode? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (SetProperty(ref _selectedMenuItem, value))
            {
                OnPropertyChanged(nameof(SelectedMenuTitle));
            }
        }
    }

    public string SelectedMenuTitle => SelectedMenuItem?.Title ?? "None";

    public ObservableCollection<ActiveRadioOption> AvailableRadios
    {
        get => _availableRadios;
        private set => SetProperty(ref _availableRadios, value);
    }

    public ActiveRadioOption? SelectedActiveRadio
    {
        get => _selectedActiveRadio;
        set
        {
            if (!SetProperty(ref _selectedActiveRadio, value))
                return;

            _rigCatalogStore.SetActiveRig(value?.RigNum);
            OnPropertyChanged(nameof(ActiveRadioSummary));
        }
    }

    public string ActiveRadioSummary
        => SelectedActiveRadio is null
            ? "Active Radio: none"
            : $"Active Radio: {SelectedActiveRadio.Display}";

    private void OnRigCatalogStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RigCatalogStore.Entries) or nameof(RigCatalogStore.ActiveRigNum))
            RefreshActiveRadioOptions();
    }

    private void RefreshActiveRadioOptions()
    {
        var options = _rigCatalogStore.Entries
            .Select(entry => new ActiveRadioOption(entry.RigNum, $"{entry.RigNum} - {entry.Mfg} {entry.Model}"))
            .ToList();

        AvailableRadios = new ObservableCollection<ActiveRadioOption>(options);

        if (_rigCatalogStore.ActiveRigNum is int activeRigNum)
            SelectedActiveRadio = AvailableRadios.FirstOrDefault(x => x.RigNum == activeRigNum);
        else
            SelectedActiveRadio = AvailableRadios.FirstOrDefault();

        OnPropertyChanged(nameof(ActiveRadioSummary));
    }

    public void Dispose()
    {
        _rigCatalogStore.PropertyChanged -= OnRigCatalogStorePropertyChanged;
    }
}

public sealed class ActiveRadioOption
{
    public ActiveRadioOption(int rigNum, string display)
    {
        RigNum = rigNum;
        Display = display;
    }

    public int RigNum { get; }
    public string Display { get; }

    public override string ToString() => Display;
}

public sealed class MenuNode
{
    public MenuNode(string title, params MenuNode[] children)
        : this(title, false, children)
    {
    }

    public MenuNode(string title, bool isExpanded, params MenuNode[] children)
    {
        Title = title;
        IsExpanded = isExpanded;
        Children = children;
    }

    public string Title { get; }
    public bool IsExpanded { get; set; }
    public MenuNode[] Children { get; }
    public bool HasChildren => Children.Length > 0;
}
