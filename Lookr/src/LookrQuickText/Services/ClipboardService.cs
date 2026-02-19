using System.Windows;
using System.Runtime.InteropServices;
using System.Threading;

namespace LookrQuickText.Services;

public sealed class ClipboardService : IClipboardService
{
    private const int MaxClipboardAttempts = 4;
    private const int RetryDelayMilliseconds = 50;

    public void CopyText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Exception? lastError = null;

        for (var attempt = 0; attempt < MaxClipboardAttempts; attempt++)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return;
            }
            catch (COMException ex)
            {
                lastError = ex;
            }
            catch (ExternalException ex)
            {
                lastError = ex;
            }

            Thread.Sleep(RetryDelayMilliseconds);
        }

        throw new InvalidOperationException(
            "Could not access the clipboard because it is busy. Please try again.",
            lastError);
    }
}
