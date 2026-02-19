using LookrQuickText.ViewModels;

namespace LookrQuickText.Models;

public sealed class QuickTextSnippet : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _title = "New Snippet";
    private string _content = string.Empty;
    private string _category = "General";
    private string _keywords = string.Empty;
    private DateTime _lastUsedUtc = DateTime.UtcNow;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                OnPropertyChanged(nameof(Preview));
            }
        }
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string Keywords
    {
        get => _keywords;
        set
        {
            if (SetProperty(ref _keywords, value))
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
