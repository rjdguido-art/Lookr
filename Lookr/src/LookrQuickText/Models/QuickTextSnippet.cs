using LookrQuickText.ViewModels;

namespace LookrQuickText.Models;

public sealed class QuickTextSnippet : ObservableObject
{
    private const string DefaultTitle = "New Snippet";
    private const string DefaultCategory = "General";

    private string _id = Guid.NewGuid().ToString("N");
    private string _title = DefaultTitle;
    private string _content = string.Empty;
    private string _category = DefaultCategory;
    private string _keywords = string.Empty;
    private DateTime _lastUsedUtc = DateTime.UtcNow;

    public string Id
    {
        get => _id;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? Guid.NewGuid().ToString("N")
                : value.Trim();

            SetProperty(ref _id, normalized);
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value ?? string.Empty);
    }

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(Preview));
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? DefaultCategory
                : value.Trim();

            SetProperty(ref _category, normalized);
        }
    }

    public string Keywords
    {
        get => _keywords;
        set
        {
            if (SetProperty(ref _keywords, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(Tags));
            }
        }
    }

    // Backward-compatible alias for legacy data files and bindings.
    public string Tags
    {
        get => Keywords;
        set => Keywords = value;
    }

    public DateTime LastUsedUtc
    {
        get => _lastUsedUtc;
        set => SetProperty(ref _lastUsedUtc, value);
    }

    public string Preview =>
        string.IsNullOrWhiteSpace(Content)
            ? "(empty)"
            : Content.Length <= 90
                ? Content
                : $"{Content[..90]}...";
}
