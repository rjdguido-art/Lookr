using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LookrQuickText.Services;
using LookrQuickText.ViewModels;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace LookrQuickText;

public partial class App : System.Windows.Application
{
    private const string AppStorageFolderName = "LookrQuickText";

    private WinForms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private WidgetWindow? _widgetWindow;
    private bool _isHandlingFatalError;

    public MainViewModel? MainViewModel { get; private set; }

    public bool IsShuttingDown { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterUnhandledExceptionHandlers();

        try
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
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnregisterUnhandledExceptionHandlers();
        CleanupTrayIcon();
        base.OnExit(e);
    }

    private void SetupTrayIcon()
    {
        try
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
        catch (Exception ex)
        {
            WriteFatalLog(ex);
            _notifyIcon = null;
        }
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

    private void RegisterUnhandledExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
    }

    private void UnregisterUnhandledExceptionHandlers()
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnTaskSchedulerUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        HandleFatalException(e.Exception);
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException("An unknown unhandled exception occurred.");

        HandleFatalException(exception);
    }

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        WriteFatalLog(e.Exception);
    }

    private void HandleFatalException(Exception exception)
    {
        if (_isHandlingFatalError)
        {
            return;
        }

        _isHandlingFatalError = true;
        IsShuttingDown = true;

        var logPath = WriteFatalLog(exception);

        try
        {
            CleanupTrayIcon();

            var message = string.IsNullOrWhiteSpace(logPath)
                ? "Lookr QuickText failed to start.\n\nNo diagnostic log could be written."
                : $"Lookr QuickText failed to start.\n\nA diagnostic log was written to:\n{logPath}";

            WinForms.MessageBox.Show(
                message,
                "Lookr QuickText Error",
                WinForms.MessageBoxButtons.OK,
                WinForms.MessageBoxIcon.Error);
        }
        finally
        {
            if (Dispatcher.CheckAccess())
            {
                Shutdown(-1);
            }
            else
            {
                Dispatcher.Invoke(() => Shutdown(-1));
            }
        }
    }

    private static string WriteFatalLog(Exception exception)
    {
        try
        {
            var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logsDirectory = Path.Combine(appDataRoot, AppStorageFolderName, "logs");
            Directory.CreateDirectory(logsDirectory);

            var logPath = Path.Combine(logsDirectory, $"fatal-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
            var builder = new StringBuilder();
            builder.AppendLine($"TimestampUtc: {DateTime.UtcNow:O}");
            builder.AppendLine($"ProcessPath: {Environment.ProcessPath ?? "(unknown)"}");
            builder.AppendLine($"OSVersion: {Environment.OSVersion}");
            builder.AppendLine($".NETVersion: {Environment.Version}");
            builder.AppendLine();
            builder.AppendLine(exception.ToString());

            File.WriteAllText(logPath, builder.ToString());
            return logPath;
        }
        catch
        {
            return string.Empty;
        }
    }
}
