using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using LookrQuickText.Models;

namespace LookrQuickText.Services;

public sealed class SecureSnippetStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("LookrQuickText.LocalVault.v1");

    private readonly string _storageFilePath;
    public string? LastLoadError { get; private set; }

    public SecureSnippetStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LookrQuickText");

        Directory.CreateDirectory(root);
        _storageFilePath = Path.Combine(root, "snippets.bin");
    }

    public IReadOnlyList<QuickTextSnippet> Load()
    {
        LastLoadError = null;

        if (!File.Exists(_storageFilePath))
        {
            return Array.Empty<QuickTextSnippet>();
        }

        try
        {
            var encrypted = File.ReadAllBytes(_storageFilePath);
            if (encrypted.Length == 0)
            {
                return Array.Empty<QuickTextSnippet>();
            }

            var rawJson = ProtectedData.Unprotect(
                encrypted,
                Entropy,
                DataProtectionScope.CurrentUser);

            var records = JsonSerializer.Deserialize<List<SnippetRecord>>(rawJson, SerializerOptions)
                ?? new List<SnippetRecord>();

            return records.Select(record => record.ToSnippet()).ToList();
        }
        catch (CryptographicException)
        {
            LastLoadError = "Could not decrypt saved snippets for this Windows user.";
            return Array.Empty<QuickTextSnippet>();
        }
        catch (JsonException)
        {
            LastLoadError = "Saved snippets file is corrupted or invalid JSON.";
            return Array.Empty<QuickTextSnippet>();
        }
        catch (IOException)
        {
            LastLoadError = "Saved snippets file could not be read due to an I/O error.";
            return Array.Empty<QuickTextSnippet>();
        }
        catch (UnauthorizedAccessException)
        {
            LastLoadError = "Permission denied while reading saved snippets.";
            return Array.Empty<QuickTextSnippet>();
        }
        catch (Exception)
        {
            LastLoadError = "Saved snippets could not be loaded due to an unexpected error.";
            return Array.Empty<QuickTextSnippet>();
        }
    }

    public void Save(IEnumerable<QuickTextSnippet> snippets)
    {
        var records = snippets.Select(SnippetRecord.FromSnippet).ToList();
        var rawJson = JsonSerializer.SerializeToUtf8Bytes(records, SerializerOptions);

        var encrypted = ProtectedData.Protect(
            rawJson,
            Entropy,
            DataProtectionScope.CurrentUser);

        var tempPath = _storageFilePath + ".tmp";
        File.WriteAllBytes(tempPath, encrypted);
        File.Move(tempPath, _storageFilePath, true);
    }

    private sealed record SnippetRecord(
        string Id,
        string Title,
        string Content,
        string? Category,
        string? Keywords,
        string? Tags,
        DateTime LastUsedUtc)
    {
        public static SnippetRecord FromSnippet(QuickTextSnippet snippet) =>
            new(
                NormalizeId(snippet.Id),
                NormalizeTitle(snippet.Title),
                snippet.Content ?? string.Empty,
                NormalizeCategory(snippet.Category),
                snippet.Keywords ?? string.Empty,
                null,
                snippet.LastUsedUtc);

        public QuickTextSnippet ToSnippet() =>
            new()
            {
                Id = NormalizeId(Id),
                Title = NormalizeTitle(Title),
                Content = Content ?? string.Empty,
                Category = NormalizeCategory(Category),
                Keywords = string.IsNullOrWhiteSpace(Keywords) ? Tags?.Trim() ?? string.Empty : Keywords.Trim(),
                LastUsedUtc = LastUsedUtc == default ? DateTime.UtcNow : LastUsedUtc
            };

        private static string NormalizeId(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Guid.NewGuid().ToString("N")
                : value.Trim();
        }

        private static string NormalizeTitle(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Untitled QuickText"
                : value.Trim();
        }

        private static string NormalizeCategory(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "General"
                : value.Trim();
        }
    }
}
