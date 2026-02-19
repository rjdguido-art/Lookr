using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LookrQuickText.Models;
using LookrQuickText.ViewModels;

namespace LookrQuickText;

public partial class WidgetWindow : Window
{
    private readonly Action _openLibrary;
    private bool _allowClose;
    private bool _positionInitialized;

    public WidgetWindow(MainViewModel viewModel, Action openLibrary)
    {
        InitializeComponent();
        DataContext = viewModel;
        _openLibrary = openLibrary;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_positionInitialized)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Right - Width - 16, workArea.Left + 8);
        Top = Math.Max(workArea.Bottom - Height - 16, workArea.Top + 8);

        _positionInitialized = true;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void OnSnippetDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (SnippetList.SelectedItem is QuickTextSnippet snippet)
        {
            viewModel.CopySnippetCommand.Execute(snippet);
        }
    }

    private void OnOpenLibraryClick(object sender, RoutedEventArgs e)
    {
        _openLibrary();
    }
}
