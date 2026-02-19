using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            return Array.Empty<QuickTextSnippet>();
        }
        catch (JsonException)
        {
            return Array.Empty<QuickTextSnippet>();
        }
        catch (IOException)
        {
            return Array.Empty<QuickTextSnippet>();
        }
        catch (UnauthorizedAccessException)
        {
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
                snippet.Id,
                snippet.Title,
                snippet.Content,
                snippet.Category,
                snippet.Keywords,
                null,
                snippet.LastUsedUtc);

        public QuickTextSnippet ToSnippet() =>
            new()
            {
                Id = Id,
                Title = Title,
                Content = Content,
                Category = string.IsNullOrWhiteSpace(Category) ? "General" : Category,
                Keywords = string.IsNullOrWhiteSpace(Keywords) ? Tags ?? string.Empty : Keywords,
                LastUsedUtc = LastUsedUtc
            };
    }
}
