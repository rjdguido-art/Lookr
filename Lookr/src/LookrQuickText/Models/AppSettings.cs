namespace LookrQuickText.Models;

public sealed class AppSettings
{
    public string AiExecutablePath { get; set; } = string.Empty;

    public string AiModelPath { get; set; } = string.Empty;

    public double AiTemperature { get; set; } = 0.7;

    public int AiMaxTokens { get; set; } = 220;
}
