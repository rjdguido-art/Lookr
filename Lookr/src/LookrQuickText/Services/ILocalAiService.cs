using LookrQuickText.Models;

namespace LookrQuickText.Services;

public interface ILocalAiService
{
    Task<string> GenerateQuickTextAsync(
        LocalAiRuntimeSettings settings,
        LocalAiGenerationRequest request,
        CancellationToken cancellationToken);
}
