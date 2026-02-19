using System.Text.Json;
using System.IO;
using LookrQuickText.Models;

namespace LookrQuickText.Services;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;
    public string? LastLoadError { get; private set; }

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
        LastLoadError = null;

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
            LastLoadError = "App settings file could not be read due to an I/O error.";
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            LastLoadError = "Permission denied while reading app settings.";
            return new AppSettings();
        }
        catch (JsonException)
        {
            LastLoadError = "App settings file is corrupted or invalid JSON.";
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
