namespace MenuManager.Client.Services;

public enum AppTheme { Light, Dark, Custom }

public class ThemeState
{
    private AppTheme _theme = AppTheme.Light;

    public AppTheme Theme => _theme;
    public bool IsDarkMode => _theme != AppTheme.Light;

    public event Action? OnChange;

    public void Set(AppTheme theme)
    {
        _theme = theme;
        OnChange?.Invoke();
    }

    public void Cycle()
    {
        _theme = _theme switch
        {
            AppTheme.Light  => AppTheme.Dark,
            AppTheme.Dark   => AppTheme.Custom,
            AppTheme.Custom => AppTheme.Light,
            _               => AppTheme.Light
        };
        OnChange?.Invoke();
    }
}
