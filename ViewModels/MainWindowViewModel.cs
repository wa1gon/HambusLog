namespace HamBusLog.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MenuNode[] MenuItems { get; } =
    [
        new MenuNode("Grid"),
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

    public MainWindowViewModel()
    {
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
