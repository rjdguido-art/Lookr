using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;
using System.Windows.Threading;
using LookrQuickText.Models;
using LookrQuickText.Services;

namespace LookrQuickText.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const string AllCategoriesLabel = "All Categories";

    private readonly SecureSnippetStore _snippetStore;
    private readonly AppSettingsStore _settingsStore;
    private readonly ExcelSnippetImporter _excelImporter;
    private readonly ILocalAiService _localAiService;
    private readonly IClipboardService _clipboard;
    private readonly DispatcherTimer _autoSaveTimer;
    private readonly DispatcherTimer _settingsSaveTimer;

    private QuickTextSnippet? _selectedSnippet;
    private string _searchQuery = string.Empty;
    private string _widgetSearchQuery = string.Empty;
    private string _selectedCategoryFilter = AllCategoriesLabel;
    private string _widgetSelectedCategoryFilter = AllCategoriesLabel;
    private string _statusMessage = "Ready";

    private string _aiPrompt = "Write a concise professional follow-up message.";
    private string _aiOutput = string.Empty;
    private string _aiExecutablePath = string.Empty;
    private string _aiModelPath = string.Empty;
    private double _aiTemperature = 0.7;
    private int _aiMaxTokens = 220;
    private bool _isAiBusy;

    public MainViewModel(
        SecureSnippetStore snippetStore,
        AppSettingsStore settingsStore,
        ExcelSnippetImporter excelImporter,
        ILocalAiService localAiService,
        IClipboardService clipboard)
    {
        _snippetStore = snippetStore;
        _settingsStore = settingsStore;
        _excelImporter = excelImporter;
        _localAiService = localAiService;
        _clipboard = clipboard;

        AddSnippetCommand = new RelayCommand(_ => AddSnippet());
        SaveLibraryCommand = new RelayCommand(_ => SaveLibrary());
        DeleteSnippetCommand = new RelayCommand(_ => DeleteSelectedSnippet(), _ => SelectedSnippet is not null);
        CopySnippetCommand = new RelayCommand(CopySnippet, CanCopySnippet);
        ToggleWidgetCommand = new RelayCommand(_ => ToggleWidgetRequested?.Invoke());
        GenerateAiCommand = new RelayCommand(_ => _ = GenerateAiAsync(), _ => CanGenerateAi());
        ApplyAiOutputToSnippetCommand = new RelayCommand(_ => ApplyAiOutputToSnippet(), _ => CanApplyAiOutputToSnippet());
        CreateSnippetFromAiCommand = new RelayCommand(_ => CreateSnippetFromAiOutput(), _ => CanCreateSnippetFromAiOutput());

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _autoSaveTimer.Tick += (_, _) =>
        {
            _autoSaveTimer.Stop();
            SaveLibrary();
        };

        _settingsSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _settingsSaveTimer.Tick += (_, _) =>
        {
            _settingsSaveTimer.Stop();
            SaveSettings();
        };

        CategoryFilters.Add(AllCategoriesLabel);
        WidgetCategoryFilters.Add(AllCategoriesLabel);

        LoadSettings();
        LoadSnippets();
    }

    public event Action? ToggleWidgetRequested;

    public ObservableCollection<QuickTextSnippet> Snippets { get; } = new();

    public ObservableCollection<QuickTextSnippet> FilteredSnippets { get; } = new();

    public ObservableCollection<QuickTextSnippet> WidgetFilteredSnippets { get; } = new();

    public ObservableCollection<string> CategoryFilters { get; } = new();

    public ObservableCollection<string> WidgetCategoryFilters { get; } = new();

    public RelayCommand AddSnippetCommand { get; }

    public RelayCommand SaveLibraryCommand { get; }

    public RelayCommand DeleteSnippetCommand { get; }

    public RelayCommand CopySnippetCommand { get; }

    public RelayCommand ToggleWidgetCommand { get; }

    public RelayCommand GenerateAiCommand { get; }

    public RelayCommand ApplyAiOutputToSnippetCommand { get; }

    public RelayCommand CreateSnippetFromAiCommand { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value))
            {
                return;
            }

            RefreshFilteredSnippets();
        }
    }

    public string WidgetSearchQuery
    {
        get => _widgetSearchQuery;
        set
        {
            if (!SetProperty(ref _widgetSearchQuery, value))
            {
                return;
            }

            RefreshWidgetFilteredSnippets();
        }
    }

    public string SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (!SetProperty(ref _selectedCategoryFilter, value))
            {
                return;
            }

            RefreshFilteredSnippets();
        }
    }

    public string WidgetSelectedCategoryFilter
    {
        get => _widgetSelectedCategoryFilter;
        set
        {
            if (!SetProperty(ref _widgetSelectedCategoryFilter, value))
            {
                return;
            }

            RefreshWidgetFilteredSnippets();
        }
    }

    public QuickTextSnippet? SelectedSnippet
    {
        get => _selectedSnippet;
        set
        {
            if (!SetProperty(ref _selectedSnippet, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelection));
            DeleteSnippetCommand.RaiseCanExecuteChanged();
            CopySnippetCommand.RaiseCanExecuteChanged();
            GenerateAiCommand.RaiseCanExecuteChanged();
            ApplyAiOutputToSnippetCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasSelection => SelectedSnippet is not null;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string AiPrompt
    {
        get => _aiPrompt;
        set
        {
            if (!SetProperty(ref _aiPrompt, value))
            {
                return;
            }

            GenerateAiCommand.RaiseCanExecuteChanged();
        }
    }

    public string AiOutput
    {
        get => _aiOutput;
        private set
        {
            if (!SetProperty(ref _aiOutput, value))
            {
                return;
            }

            ApplyAiOutputToSnippetCommand.RaiseCanExecuteChanged();
            CreateSnippetFromAiCommand.RaiseCanExecuteChanged();
        }
    }

    public string AiExecutablePath
    {
        get => _aiExecutablePath;
        set
        {
            if (!SetProperty(ref _aiExecutablePath, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsAiConfigured));
            GenerateAiCommand.RaiseCanExecuteChanged();
            QueueSettingsSave();
        }
    }

    public string AiModelPath
    {
        get => _aiModelPath;
        set
        {
            if (!SetProperty(ref _aiModelPath, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsAiConfigured));
            GenerateAiCommand.RaiseCanExecuteChanged();
            QueueSettingsSave();
        }
    }

    public double AiTemperature
    {
        get => _aiTemperature;
        set
        {
            var normalized = Math.Clamp(value, 0.1, 1.5);
            if (!SetProperty(ref _aiTemperature, normalized))
            {
                return;
            }

            QueueSettingsSave();
        }
    }

    public int AiMaxTokens
    {
        get => _aiMaxTokens;
        set
        {
            var normalized = Math.Clamp(value, 64, 1024);
            if (!SetProperty(ref _aiMaxTokens, normalized))
            {
                return;
            }

            QueueSettingsSave();
        }
    }

    public bool IsAiBusy
    {
        get => _isAiBusy;
        private set
        {
            if (!SetProperty(ref _isAiBusy, value))
            {
                return;
            }

            GenerateAiCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsAiConfigured =>
        File.Exists(AiExecutablePath)
        && File.Exists(AiModelPath);

    public void PersistNow()
    {
        SaveLibrary();
        SaveSettings();
    }

    public ExcelImportResult ImportFromExcelTemplate(string filePath)
    {
        var imported = _excelImporter.Import(filePath);
        if (imported.Count == 0)
        {
            StatusMessage = "No quicktexts found in the selected workbook.";
            return new ExcelImportResult(0, 0);
        }

        var existing = new HashSet<string>(
            Snippets.Select(BuildIdentityKey),
            StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var skipped = 0;
        QuickTextSnippet? firstAdded = null;

        foreach (var snippet in imported)
        {
            var identity = BuildIdentityKey(snippet);
            if (existing.Contains(identity))
            {
                skipped++;
                continue;
            }

            existing.Add(identity);
            AttachSnippet(snippet);
            Snippets.Add(snippet);

            firstAdded ??= snippet;
            added++;
        }

        if (firstAdded is not null)
        {
            SelectedSnippet = firstAdded;
        }

        RefreshCategoryFilters();
        RefreshFilteredSnippets();
        RefreshWidgetFilteredSnippets();
        QueueAutoSave();

        StatusMessage = $"Imported {added} quicktexts from Excel.";
        return new ExcelImportResult(added, skipped);
    }

    private static string BuildIdentityKey(QuickTextSnippet snippet)
    {
        var title = snippet.Title.Trim().ToLowerInvariant();
        var content = snippet.Content.Trim().ToLowerInvariant();
        return $"{title}::{content}";
    }

    private void LoadSettings()
    {
        var settings = _settingsStore.Load();
        if (!string.IsNullOrWhiteSpace(_settingsStore.LastLoadError))
        {
            StatusMessage = _settingsStore.LastLoadError;
        }

        _aiExecutablePath = settings.AiExecutablePath ?? string.Empty;
        _aiModelPath = settings.AiModelPath ?? string.Empty;
        _aiTemperature = Math.Clamp(settings.AiTemperature, 0.1, 1.5);
        _aiMaxTokens = Math.Clamp(settings.AiMaxTokens, 64, 1024);

        OnPropertyChanged(nameof(AiExecutablePath));
        OnPropertyChanged(nameof(AiModelPath));
        OnPropertyChanged(nameof(AiTemperature));
        OnPropertyChanged(nameof(AiMaxTokens));
        OnPropertyChanged(nameof(IsAiConfigured));
        GenerateAiCommand.RaiseCanExecuteChanged();
    }

    private void LoadSnippets()
    {
        var loaded = _snippetStore.Load();
        if (!string.IsNullOrWhiteSpace(_snippetStore.LastLoadError))
        {
            StatusMessage = _snippetStore.LastLoadError;
        }

        if (loaded.Count == 0)
        {
            loaded =
            [
                new QuickTextSnippet
                {
                    Title = "Welcome",
                    Content = "Use Lookr to store reusable quicktexts and copy them instantly.",
                    Category = "General",
                    Keywords = "intro,getting-started",
                    LastUsedUtc = DateTime.UtcNow
                }
            ];
        }

        foreach (var snippet in loaded)
        {
            if (string.IsNullOrWhiteSpace(snippet.Category))
            {
                snippet.Category = "General";
            }

            AttachSnippet(snippet);
            Snippets.Add(snippet);
        }

        RefreshCategoryFilters();
        RefreshFilteredSnippets();
        RefreshWidgetFilteredSnippets();
        SelectedSnippet = FilteredSnippets.FirstOrDefault();
    }

    private void AttachSnippet(QuickTextSnippet snippet)
    {
        snippet.PropertyChanged += OnSnippetChanged;
    }

    private void DetachSnippet(QuickTextSnippet snippet)
    {
        snippet.PropertyChanged -= OnSnippetChanged;
    }

    private void OnSnippetChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (
            nameof(QuickTextSnippet.Title)
            or nameof(QuickTextSnippet.Content)
            or nameof(QuickTextSnippet.Category)
            or nameof(QuickTextSnippet.Keywords)
            or nameof(QuickTextSnippet.LastUsedUtc)))
        {
            return;
        }

        if (e.PropertyName == nameof(QuickTextSnippet.Category))
        {
            RefreshCategoryFilters();
        }

        RefreshFilteredSnippets();
        RefreshWidgetFilteredSnippets();

        CopySnippetCommand.RaiseCanExecuteChanged();
        QueueAutoSave();
    }

    private void AddSnippet()
    {
        var snippet = new QuickTextSnippet
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "New QuickText",
            Category = SelectedCategoryFilter == AllCategoriesLabel ? "General" : SelectedCategoryFilter,
            LastUsedUtc = DateTime.UtcNow
        };

        AttachSnippet(snippet);
        Snippets.Add(snippet);
        SelectedSnippet = snippet;

        RefreshCategoryFilters();
        RefreshFilteredSnippets();
        RefreshWidgetFilteredSnippets();
        QueueAutoSave();

        StatusMessage = "Created a new quicktext.";
    }

    private void DeleteSelectedSnippet()
    {
        if (SelectedSnippet is null)
        {
            return;
        }

        DetachSnippet(SelectedSnippet);
        Snippets.Remove(SelectedSnippet);

        RefreshCategoryFilters();
        RefreshFilteredSnippets();
        RefreshWidgetFilteredSnippets();

        SelectedSnippet = FilteredSnippets.FirstOrDefault();
        QueueAutoSave();

        StatusMessage = "Quicktext deleted.";
    }

    private bool CanCopySnippet(object? parameter)
    {
        var snippet = parameter as QuickTextSnippet ?? SelectedSnippet;
        return snippet is not null && !string.IsNullOrWhiteSpace(snippet.Content);
    }

    private void CopySnippet(object? parameter)
    {
        var snippet = parameter as QuickTextSnippet ?? SelectedSnippet;
        if (snippet is null || string.IsNullOrWhiteSpace(snippet.Content))
        {
            return;
        }

        _clipboard.CopyText(snippet.Content);
        snippet.LastUsedUtc = DateTime.UtcNow;

        QueueAutoSave();
        StatusMessage = $"Copied '{snippet.Title}' to clipboard.";
    }

    private bool CanGenerateAi()
    {
        return !IsAiBusy
            && !string.IsNullOrWhiteSpace(AiPrompt)
            && !string.IsNullOrWhiteSpace(AiExecutablePath)
            && !string.IsNullOrWhiteSpace(AiModelPath)
            && File.Exists(AiExecutablePath)
            && File.Exists(AiModelPath);
    }

    private async Task GenerateAiAsync()
    {
        if (!CanGenerateAi())
        {
            StatusMessage = "Set the AI executable, model path, and prompt first.";
            return;
        }

        IsAiBusy = true;
        StatusMessage = "Generating quicktext locally...";

        try
        {
            var request = new LocalAiGenerationRequest(
                Prompt: AiPrompt,
                Category: SelectedSnippet?.Category ?? string.Empty,
                Keywords: SelectedSnippet?.Keywords ?? string.Empty,
                ExistingText: SelectedSnippet?.Content ?? string.Empty);

            var runtime = new LocalAiRuntimeSettings(
                ExecutablePath: AiExecutablePath,
                ModelPath: AiModelPath,
                Temperature: AiTemperature,
                MaxTokens: AiMaxTokens);

            var generated = await _localAiService.GenerateQuickTextAsync(runtime, request, CancellationToken.None);
            AiOutput = generated.Trim();
            StatusMessage = "AI quicktext generated successfully.";
        }
        catch (Exception ex)
        {
            AiOutput = string.Empty;
            StatusMessage = $"AI generation failed: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
        }
    }

    private bool CanApplyAiOutputToSnippet()
    {
        return SelectedSnippet is not null && !string.IsNullOrWhiteSpace(AiOutput);
    }

    private void ApplyAiOutputToSnippet()
    {
        if (SelectedSnippet is null || string.IsNullOrWhiteSpace(AiOutput))
        {
            return;
        }

        SelectedSnippet.Content = AiOutput.Trim();
        SelectedSnippet.LastUsedUtc = DateTime.UtcNow;

        QueueAutoSave();
        StatusMessage = "Applied AI output to selected quicktext.";
    }

    private bool CanCreateSnippetFromAiOutput()
    {
        return !string.IsNullOrWhiteSpace(AiOutput);
    }

    private void CreateSnippetFromAiOutput()
    {
        if (string.IsNullOrWhiteSpace(AiOutput))
        {
            return;
        }

        var suggestedTitle = BuildTitleFromPrompt(AiPrompt);
        var snippet = new QuickTextSnippet
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = suggestedTitle,
            Content = AiOutput.Trim(),
            Category = SelectedSnippet?.Category ?? "General",
            Keywords = SelectedSnippet?.Keywords ?? string.Empty,
            LastUsedUtc = DateTime.UtcNow
        };

        AttachSnippet(snippet);
        Snippets.Add(snippet);
        SelectedSnippet = snippet;

        RefreshCategoryFilters();
        RefreshFilteredSnippets();
        RefreshWidgetFilteredSnippets();
        QueueAutoSave();

        StatusMessage = "Created a new quicktext from AI output.";
    }

    private static string BuildTitleFromPrompt(string prompt)
    {
        var normalized = prompt?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "AI QuickText";
        }

        return normalized.Length <= 38
            ? normalized
            : normalized[..38] + "...";
    }

    private void SaveLibrary()
    {
        try
        {
            _snippetStore.Save(Snippets);
        }
        catch (IOException)
        {
            StatusMessage = "Could not save snippets right now. Please try again.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Permission issue while saving snippets.";
        }
        catch (CryptographicException)
        {
            StatusMessage = "Encryption issue while saving snippets.";
        }
    }

    private void QueueAutoSave()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void QueueSettingsSave()
    {
        _settingsSaveTimer.Stop();
        _settingsSaveTimer.Start();
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            AiExecutablePath = AiExecutablePath,
            AiModelPath = AiModelPath,
            AiTemperature = AiTemperature,
            AiMaxTokens = AiMaxTokens
        };

        try
        {
            _settingsStore.Save(settings);
        }
        catch (IOException)
        {
            StatusMessage = "Could not save app settings right now.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Permission issue while saving app settings.";
        }
    }

    private void RefreshCategoryFilters()
    {
        var categories = Snippets
            .Select(snippet => snippet.Category?.Trim() ?? string.Empty)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var all = new List<string> { AllCategoriesLabel };
        all.AddRange(categories);

        ReplaceCollection(CategoryFilters, all);
        ReplaceCollection(WidgetCategoryFilters, all);

        if (!CategoryFilters.Contains(SelectedCategoryFilter))
        {
            SelectedCategoryFilter = AllCategoriesLabel;
        }

        if (!WidgetCategoryFilters.Contains(WidgetSelectedCategoryFilter))
        {
            WidgetSelectedCategoryFilter = AllCategoriesLabel;
        }
    }

    private void RefreshFilteredSnippets()
    {
        var selected = SelectedSnippet;

        var filtered = ApplySearchAndCategory(
                Snippets,
                SearchQuery,
                SelectedCategoryFilter)
            .OrderByDescending(snippet => snippet.LastUsedUtc)
            .ThenBy(snippet => snippet.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ReplaceCollection(FilteredSnippets, filtered);

        if (selected is not null && FilteredSnippets.Contains(selected))
        {
            return;
        }

        SelectedSnippet = FilteredSnippets.FirstOrDefault();
    }

    private void RefreshWidgetFilteredSnippets()
    {
        var filtered = ApplySearchAndCategory(
                Snippets,
                WidgetSearchQuery,
                WidgetSelectedCategoryFilter)
            .OrderByDescending(snippet => snippet.LastUsedUtc)
            .ThenBy(snippet => snippet.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ReplaceCollection(WidgetFilteredSnippets, filtered);
    }

    private static IEnumerable<QuickTextSnippet> ApplySearchAndCategory(
        IEnumerable<QuickTextSnippet> snippets,
        string query,
        string categoryFilter)
    {
        var filtered = snippets;

        if (!string.IsNullOrWhiteSpace(categoryFilter)
            && !string.Equals(categoryFilter, AllCategoriesLabel, StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(snippet =>
                string.Equals(snippet.Category, categoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return filtered;
        }

        var normalized = query.Trim();

        return filtered.Where(snippet =>
            snippet.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || snippet.Content.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || snippet.Category.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || snippet.Keywords.Contains(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}

public sealed record ExcelImportResult(int AddedCount, int SkippedCount);
