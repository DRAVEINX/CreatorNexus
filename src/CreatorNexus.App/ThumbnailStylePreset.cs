namespace CreatorNexus.App;

public sealed record ThumbnailStylePreset
{
    public string Name { get; init; } = string.Empty;
    public string Theme { get; init; } = "Purple";
    public string TextAlignment { get; init; } = "Center";
    public string TitleSize { get; init; } = "Medium";
    public string SubtitleSize { get; init; } = "Medium";
    public string FontFamily { get; init; } = "Arial";
    public string Layout { get; init; } = "Center";
    public string TextColor { get; init; } = "White";
    public string TextOutlineColor { get; init; } = "Black";
    public string TextOutlineThickness { get; init; } = "Medium";
    public string TextPanelStyle { get; init; } = "Dark";
    public string TextPanelOpacity { get; init; } = "Medium";
    public string TextPanelPadding { get; init; } = "Normal";
    public string TextPanelCorners { get; init; } = "Rounded";
    public string TextPanelBorder { get; init; } = "None";
    public string TextPanelBorderColor { get; init; } = "White";
    public string TextPanelBorderThickness { get; init; } = "Medium";
    public string TextPanelBorderOpacity { get; init; } = "Strong";
    public string TextPlacement { get; init; } = "Center";
    public string ImageFit { get; init; } = "Cover";
}
