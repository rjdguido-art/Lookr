namespace LookrQuickText.Models;

public sealed record LocalAiRuntimeSettings(
    string ExecutablePath,
    string ModelPath,
    double Temperature,
    int MaxTokens);
