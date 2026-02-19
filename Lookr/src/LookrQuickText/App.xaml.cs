using System.Windows;
using LookrQuickText.Services;
using LookrQuickText.ViewModels;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace LookrQuickText;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private WidgetWindow? _widgetWindow;

    public MainViewModel? MainViewModel { get; private set; }

    public bool IsShuttingDown { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var snippetStore = new SecureSnippetStore();
        var settingsStore = new AppSettingsStore();
        var excelImporter = new ExcelSnippetImporter();
        var localAiService = new LlamaCppAiService();
        var clipboard = new ClipboardService();
        MainViewModel = new MainViewModel(
            snippetStore,
            settingsStore,
            excelImporter,
            localAiService,
            clipboard);
        MainViewModel.ToggleWidgetRequested += ToggleWidget;

        _mainWindow = new MainWindow(MainViewModel);
        _mainWindow.Show();

        SetupTrayIcon();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CleanupTrayIcon();
        base.OnExit(e);
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "Lookr QuickText",
            Visible = true
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Open Library", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Toggle Widget", null, (_, _) => ToggleWidget());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ToggleWidget();
    }

    private void CleanupTrayIcon()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    public void ToggleWidget()
    {
        if (MainViewModel is null)
        {
            return;
        }

        _widgetWindow ??= new WidgetWindow(MainViewModel, ShowMainWindow);

        if (_widgetWindow.IsVisible)
        {
            _widgetWindow.Hide();
            return;
        }

        _widgetWindow.Show();
        _widgetWindow.Activate();
    }

    private void ExitApplication()
    {
        if (IsShuttingDown)
        {
            return;
        }

        IsShuttingDown = true;

        MainViewModel?.PersistNow();

        if (_widgetWindow is not null)
        {
            _widgetWindow.ForceClose();
            _widgetWindow = null;
        }

        _mainWindow?.Close();

        CleanupTrayIcon();
        Shutdown();
    }
}
