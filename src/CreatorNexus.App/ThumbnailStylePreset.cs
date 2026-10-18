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
    /// <summary>Rejects malformed or unsupported values before a preset enters the style library.</summary>
    public bool IsValid()
    {
        static bool IsOneOf(string? value, params string[] allowed) =>
            value is not null && allowed.Contains(value, StringComparer.Ordinal);

        return !string.IsNullOrWhiteSpace(Name) && Name.Trim().Length <= 50
            && IsOneOf(Theme, "Purple", "Blue", "Orange", "Green")
            && IsOneOf(TextAlignment, "Center", "Left", "Right")
            && IsOneOf(TitleSize, "Small", "Medium", "Large")
            && IsOneOf(SubtitleSize, "Small", "Medium", "Large")
            && IsOneOf(FontFamily, "Arial", "Georgia", "Courier New")
            && IsOneOf(Layout, "Center", "Bottom banner", "Top banner")
            && IsOneOf(TextColor, "White", "Yellow", "Black")
            && IsOneOf(TextOutlineColor, "None", "Black", "White")
            && IsOneOf(TextOutlineThickness, "Thin", "Medium", "Thick")
            && IsOneOf(TextPanelStyle, "None", "Dark", "Light", "Theme")
            && IsOneOf(TextPanelOpacity, "Subtle", "Medium", "Strong")
            && IsOneOf(TextPanelPadding, "Compact", "Normal", "Spacious")
            && IsOneOf(TextPanelCorners, "Square", "Soft", "Rounded")
            && IsOneOf(TextPanelBorder, "None", "Subtle", "Strong")
            && IsOneOf(TextPanelBorderColor, "White", "Black", "Theme")
            && IsOneOf(TextPanelBorderThickness, "Thin", "Medium", "Thick")
            && IsOneOf(TextPanelBorderOpacity, "Subtle", "Medium", "Strong")
            && IsOneOf(TextPlacement, "Top", "Center", "Bottom")
            && IsOneOf(ImageFit, "Cover", "Fit");
    }
}
