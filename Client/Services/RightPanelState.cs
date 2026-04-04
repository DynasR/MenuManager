using Microsoft.AspNetCore.Components;

namespace MenuManager.Client.Services;

public class RightPanelState
{
    public RenderFragment? Content { get; private set; }
    public bool IsOpen { get; private set; }

    public event Action? OnChange;

    public void SetContent(RenderFragment? content)
    {
        Content = content;
        NotifyStateChanged();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        NotifyStateChanged();
    }

    public void Open()
    {
        IsOpen = true;
        NotifyStateChanged();
    }

    public void Close()
    {
        IsOpen = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
