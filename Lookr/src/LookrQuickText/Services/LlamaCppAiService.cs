using System.Diagnostics;
using System.Globalization;
using System.Text;
using LookrQuickText.Models;

namespace LookrQuickText.Services;

public sealed class LlamaCppAiService : ILocalAiService
{
    public async Task<string> GenerateQuickTextAsync(
        LocalAiRuntimeSettings settings,
        LocalAiGenerationRequest request,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.ExecutablePath))
        {
            throw new FileNotFoundException("The llama executable was not found.", settings.ExecutablePath);
        }

        if (!File.Exists(settings.ModelPath))
        {
            throw new FileNotFoundException("The GGUF model file was not found.", settings.ModelPath);
        }

        var prompt = BuildPrompt(request);
        var attempt = await RunProcessAsync(
            settings.ExecutablePath,
            BuildArguments(settings, prompt, includeNoDisplayPrompt: true),
            cancellationToken);

        if (attempt.ExitCode != 0
            && ContainsUnknownOption(attempt.Error, "--no-display-prompt"))
        {
            attempt = await RunProcessAsync(
                settings.ExecutablePath,
                BuildArguments(settings, prompt, includeNoDisplayPrompt: false),
                cancellationToken);
        }

        if (attempt.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(attempt.Error)
                ? "Local AI process failed."
                : attempt.Error.Trim();

            throw new InvalidOperationException(message);
        }

        var cleaned = CleanOutput(attempt.Output);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new InvalidOperationException("Local AI returned no text.");
        }

        return cleaned;
    }

    private static string BuildPrompt(LocalAiGenerationRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You generate ready-to-paste quicktexts.");
        builder.AppendLine("Return only the final quicktext with no markdown and no explanation.");
        builder.AppendLine();
        builder.AppendLine($"Request: {request.Prompt.Trim()}");

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            builder.AppendLine($"Category hint: {request.Category.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Keywords))
        {
            builder.AppendLine($"Keywords hint: {request.Keywords.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.ExistingText))
        {
            builder.AppendLine("Existing text to improve:");
            builder.AppendLine(request.ExistingText.Trim());
        }

        builder.AppendLine();
        builder.AppendLine("### Output:");

        return builder.ToString();
    }

    private static IReadOnlyList<string> BuildArguments(
        LocalAiRuntimeSettings settings,
        string prompt,
        bool includeNoDisplayPrompt)
    {
        var maxTokens = Math.Clamp(settings.MaxTokens, 64, 1024);
        var temperature = Math.Clamp(settings.Temperature, 0.1, 1.5)
            .ToString("0.0", CultureInfo.InvariantCulture);

        var args = new List<string>
        {
            "-m", settings.ModelPath,
            "-n", maxTokens.ToString(CultureInfo.InvariantCulture),
            "--temp", temperature
        };

        if (includeNoDisplayPrompt)
        {
            args.Add("--no-display-prompt");
        }

        args.Add("-p");
        args.Add(prompt);

        return args;
    }

    private static bool ContainsUnknownOption(string errorText, string option)
    {
        if (string.IsNullOrWhiteSpace(errorText))
        {
            return false;
        }

        return errorText.Contains(option, StringComparison.OrdinalIgnoreCase)
            && errorText.Contains("unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to launch the local AI process.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        return new ProcessResult(process.ExitCode, output, error);
    }

    private static string CleanOutput(string raw)
    {
        var lines = raw
            .Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line =>
                !line.StartsWith("main:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("llama_", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("sampling:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("build:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var combined = string.Join(Environment.NewLine, lines).Trim();
        var marker = "### Output:";
        var markerIndex = combined.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (markerIndex >= 0)
        {
            combined = combined[(markerIndex + marker.Length)..];
        }

        return combined.Trim();
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
