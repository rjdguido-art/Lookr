using System.Windows;

namespace LookrQuickText.Services;

public sealed class ClipboardService : IClipboardService
{
    public void CopyText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Clipboard.SetText(text);
    }
}
