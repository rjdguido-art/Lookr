using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using LookrQuickText.ViewModels;

namespace LookrQuickText;

public partial class MainWindow : Window
{
    private const int HotkeyId = 0x4C4F4F;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkSpace = 0x20;

    private HwndSource? _hwndSource;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        SourceInitialized += OnSourceInitialized;
        Closing += OnWindowClosing;
        Closed += OnWindowClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = (HwndSource?)PresentationSource.FromVisual(this);
        if (_hwndSource is null)
        {
            return;
        }

        _hwndSource.AddHook(WndProc);
        RegisterHotKey(_hwndSource.Handle, HotkeyId, ModControl | ModShift, VkSpace);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            if (Application.Current is App app)
            {
                app.ToggleWidget();
            }

            handled = true;
        }

        return IntPtr.Zero;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (Application.Current is not App app || app.IsShuttingDown)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_hwndSource is null)
        {
            return;
        }

        _hwndSource.RemoveHook(WndProc);
        UnregisterHotKey(_hwndSource.Handle, HotkeyId);
        _hwndSource = null;
    }

    private void OnImportExcelClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Import QuickTexts from Excel",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx|Excel Macro-Enabled Workbook (*.xlsm)|*.xlsm",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var result = viewModel.ImportFromExcelTemplate(dialog.FileName);
            MessageBox.Show(
                this,
                $"Imported: {result.AddedCount}\nSkipped duplicates: {result.SkippedCount}",
                "Excel Import Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not import the selected workbook.\n\n{ex.Message}",
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnBrowseAiExecutableClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select llama.cpp executable",
            Filter = "Executable (*.exe)|*.exe|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            viewModel.AiExecutablePath = dialog.FileName;
        }
    }

    private void OnBrowseAiModelClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select GGUF model",
            Filter = "GGUF Model (*.gguf)|*.gguf|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            viewModel.AiModelPath = dialog.FileName;
        }
    }
}
