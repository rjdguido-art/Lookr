using System.Text.Json;
using LookrQuickText.Models;

namespace LookrQuickText.Services;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public AppSettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LookrQuickText");

        Directory.CreateDirectory(root);
        _settingsFilePath = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var raw = File.ReadAllText(_settingsFilePath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(raw, SerializerOptions)
                ?? new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var raw = JsonSerializer.Serialize(settings, SerializerOptions);

        var tempPath = _settingsFilePath + ".tmp";
        File.WriteAllText(tempPath, raw);
        File.Move(tempPath, _settingsFilePath, true);
    }
}
