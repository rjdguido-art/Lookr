namespace LookrQuickText.Models;

public sealed record LocalAiGenerationRequest(
    string Prompt,
    string Category,
    string Keywords,
    string ExistingText);
