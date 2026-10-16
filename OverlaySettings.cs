namespace FiveMOverlay;

public sealed class OverlaySettings
{
    public List<ChatCommandSetting> ChatCommands { get; set; } = new()
    {
        new ChatCommandSetting { Trigger = "/br", Replacement = "/su {cursor} br" }
    };
    public OverlayProfile Overlay1 { get; set; } = new()
    {
        Message = "Overlay 1 – deine Nachricht",
        Left = 100, Top = 100
    };

    public OverlayProfile Overlay2 { get; set; } = new()
    {
        Message = "Overlay 2 – zweite Nachricht",
        Left = 100, Top = 240
    };

    public OverlayProfile Overlay3 { get; set; } = new()
    {
        Message = "Overlay 3 – dritte Nachricht",
        Left = 100, Top = 380
    };
}

public sealed class OverlayProfile
{
    public string Message { get; set; } = "Deine Nachricht steht hier.";
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 500;
    public double Height { get; set; } = 110;
    public double FontSize { get; set; } = 28;
    public bool IsLocked { get; set; } = false;
    public string TextColor { get; set; } = "#FFFFFFFF";
    public string BackgroundColor { get; set; } = "#CC151922";
}


public sealed class ChatCommandSetting
{
    public string Trigger { get; set; } = "/br";
    public string Replacement { get; set; } = "/su {cursor} br";
}
