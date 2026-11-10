namespace AxlLsp.Commands;

public class ToggleShowBoundExprAtHoverCommand : ICommand
{
    public string Identifier => "toggleShowBoundExprAtHover";

    public string Title => Settings.ShowBoundExprAtHover 
        ? "Don't show bound expressions at hover." 
        : "Show bound expressions at hover.";

    /// <inheritdoc />
    public void Execute()
    {
        Settings.ShowBoundExprAtHover = !Settings.ShowBoundExprAtHover;
    }
}