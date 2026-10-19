using SkiaSharp;
using System.Collections.ObjectModel;
using System.Text.Json;
using CreatorNexus.Core;

namespace CreatorNexus.App;

public partial class MainPage : ContentPage
{
    private readonly ProjectStore _store = new();
    private readonly ObservableCollection<CreatorProject> _visibleProjects = new();
    private bool _english;
    private bool _showArchived;
    private bool _showFavoritesOnly;
    private bool _showScheduleOnly;
    private bool _searchErrorShown;
    private int _searchRequestVersion;

    public MainPage()
    {
        InitializeComponent();
        ProjectsCollection.ItemsSource = _visibleProjects;
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        ApplyLanguage();
        _ = LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects = await _store.LoadAsync();
            RenderProjects(GetFilteredProjects(projects, ProjectSearchBar.Text));
        }
        catch (Exception ex)
        {
            // Keep the UI responsive and explain the recovery path instead of leaving
            // a fire-and-forget startup task with an unhandled exception.
            RenderProjects(Array.Empty<CreatorProject>());
            await DisplayAlertAsync(
                _english ? "Projects could not be loaded" : "Projecten konden niet worden geladen",
                _english
                    ? "CreatorNexus could not read the saved project data. Your original file has been preserved. Use 'Recover local backup' if a valid local backup exists, or restore a portable backup. Details: " + ex.Message
                    : "CreatorNexus kon de opgeslagen projectgegevens niet lezen. Het oorspronkelijke bestand is bewaard. Gebruik 'Herstel lokale back-up' als er een geldige lokale back-up bestaat, of zet een externe back-up terug. Details: " + ex.Message,
                "OK");
        }
    }

    private IEnumerable<CreatorProject> GetFilteredProjects(IEnumerable<CreatorProject> projects, string? searchText)
    {
        var filtered = projects.Where(p => p.IsArchived == _showArchived);
        if (_showFavoritesOnly)
            filtered = filtered.Where(p => p.IsFavorite);
        if (_showScheduleOnly)
            filtered = filtered.Where(p => p.ScheduledFor.HasValue);

        var searched = ProjectRules.Search(filtered, searchText);
        return _showScheduleOnly
            ? searched.OrderBy(p => p.ScheduledFor)
            : searched.OrderByDescending(p => p.IsFavorite).ThenByDescending(p => p.UpdatedAt);
    }

    private void RenderProjects(IEnumerable<CreatorProject> projects)
    {
        _visibleProjects.Clear();
        foreach (var project in projects)
            _visibleProjects.Add(project);

        ProjectCountLabel.Text = _english
            ? $"{_visibleProjects.Count} projects"
            : $"{_visibleProjects.Count} projecten";

        if (_visibleProjects.Count > 0)
            return;

        var hasSearch = !string.IsNullOrWhiteSpace(ProjectSearchBar.Text);
        if (hasSearch || _showFavoritesOnly || _showScheduleOnly)
        {
            EmptyTitleLabel.Text = _english ? "No matching projects" : "Geen projecten gevonden";
            EmptySubtitleLabel.Text = _english
                ? "Try another search term or turn off one of the active filters."
                : "Probeer een andere zoekterm of schakel een actief filter uit.";
            return;
        }

        EmptyTitleLabel.Text = _showArchived
            ? (_english ? "No archived projects" : "Geen gearchiveerde projecten")
            : (_english ? "No projects here yet" : "Hier staan nog geen projecten");
        EmptySubtitleLabel.Text = _showArchived
            ? (_english ? "Archived projects will appear here." : "Gearchiveerde projecten verschijnen hier.")
            : (_english ? "Create your first creator project above." : "Maak hierboven je eerste creatorproject.");
    }

    private static CreatorProject CopyThumbnailDesign(CreatorProject source, CreatorProject target) => target with
    {
        ThumbnailTitle = source.ThumbnailTitle,
        ThumbnailSubtitle = source.ThumbnailSubtitle,
        ThumbnailTheme = source.ThumbnailTheme,
        ThumbnailImagePath = source.ThumbnailImagePath,
        ThumbnailImageFit = source.ThumbnailImageFit,
        ThumbnailTextAlignment = source.ThumbnailTextAlignment,
        ThumbnailTitleSize = source.ThumbnailTitleSize,
        ThumbnailSubtitleSize = source.ThumbnailSubtitleSize,
        ThumbnailFontFamily = source.ThumbnailFontFamily,
        ThumbnailLayout = source.ThumbnailLayout,
        ThumbnailTextColor = source.ThumbnailTextColor,
        ThumbnailTextOutlineColor = source.ThumbnailTextOutlineColor,
        ThumbnailTextOutlineThickness = source.ThumbnailTextOutlineThickness,
        ThumbnailTextPanelStyle = source.ThumbnailTextPanelStyle,
        ThumbnailTextPanelOpacity = source.ThumbnailTextPanelOpacity,
        ThumbnailTextPanelPadding = source.ThumbnailTextPanelPadding,
        ThumbnailTextPanelCorners = source.ThumbnailTextPanelCorners,
        ThumbnailTextPanelBorder = source.ThumbnailTextPanelBorder,
        ThumbnailTextPanelBorderColor = source.ThumbnailTextPanelBorderColor,
        ThumbnailTextPanelBorderThickness = source.ThumbnailTextPanelBorderThickness,
        ThumbnailTextPanelBorderOpacity = source.ThumbnailTextPanelBorderOpacity,
        ThumbnailTextPlacement = source.ThumbnailTextPlacement,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private async void OnEditThumbnailClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;
        var styleStore = new ThumbnailStyleStore();

        var titleEntry = new Entry
        {
            Text = string.IsNullOrWhiteSpace(project.ThumbnailTitle) ? project.Title : project.ThumbnailTitle,
            Placeholder = _english ? "Thumbnail headline" : "Koptekst voor thumbnail",
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#B9B9C9"),
            BackgroundColor = Color.FromArgb("#262638"),
            MaxLength = 90
        };
        var subtitleEntry = new Entry
        {
            Text = project.ThumbnailSubtitle,
            Placeholder = _english ? "Optional subtitle" : "Optionele ondertitel",
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#B9B9C9"),
            BackgroundColor = Color.FromArgb("#262638"),
            MaxLength = 120
        };
        var themes = new[] { "Purple", "Blue", "Orange", "Green" };
        var picker = new Picker
        {
            Title = _english ? "Color theme" : "Kleurthema",
            ItemsSource = themes,
            SelectedItem = themes.Contains(project.ThumbnailTheme) ? project.ThumbnailTheme : "Purple",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var alignments = new[] { "Center", "Left", "Right" };
        var alignmentPicker = new Picker
        {
            Title = _english ? "Text alignment" : "Tekstuitlijning",
            ItemsSource = alignments,
            SelectedItem = alignments.Contains(project.ThumbnailTextAlignment) ? project.ThumbnailTextAlignment : "Center",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var titleSizes = new[] { "Small", "Medium", "Large" };
        var titleSizePicker = new Picker
        {
            Title = _english ? "Headline size" : "Grootte koptekst",
            ItemsSource = titleSizes,
            SelectedItem = titleSizes.Contains(project.ThumbnailTitleSize) ? project.ThumbnailTitleSize : "Medium",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var subtitleSizes = new[] { "Small", "Medium", "Large" };
        var fontFamilies = new[] { "Arial", "Georgia", "Courier New" };
        var fontFamilyPicker = new Picker
        {
            Title = _english ? "Font family" : "Lettertype",
            ItemsSource = fontFamilies,
            SelectedItem = fontFamilies.Contains(project.ThumbnailFontFamily) ? project.ThumbnailFontFamily : "Arial",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var subtitleSizePicker = new Picker
        {
            Title = _english ? "Subtitle size" : "Grootte ondertitel",
            ItemsSource = subtitleSizes,
            SelectedItem = subtitleSizes.Contains(project.ThumbnailSubtitleSize) ? project.ThumbnailSubtitleSize : "Medium",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var textColors = new[] { "White", "Yellow", "Black" };
        var textColorPicker = new Picker
        {
            Title = _english ? "Text color" : "Tekstkleur",
            ItemsSource = textColors,
            SelectedItem = textColors.Contains(project.ThumbnailTextColor) ? project.ThumbnailTextColor : "White",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var outlineColors = new[] { "None", "Black", "White" };
        var outlineColorPicker = new Picker
        {
            Title = _english ? "Text outline" : "Tekstomlijning",
            ItemsSource = outlineColors,
            SelectedItem = outlineColors.Contains(project.ThumbnailTextOutlineColor) ? project.ThumbnailTextOutlineColor : "Black",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var outlineThicknesses = new[] { "Thin", "Medium", "Thick" };
        var outlineThicknessPicker = new Picker
        {
            Title = _english ? "Outline thickness" : "Dikte tekstomlijning",
            ItemsSource = outlineThicknesses,
            SelectedItem = outlineThicknesses.Contains(project.ThumbnailTextOutlineThickness) ? project.ThumbnailTextOutlineThickness : "Medium",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelStyles = new[] { "None", "Dark", "Light", "Theme" };
        var panelOpacities = new[] { "Subtle", "Medium", "Strong" };
        var panelPaddings = new[] { "Compact", "Normal", "Spacious" };
        var panelCornerStyles = new[] { "Square", "Soft", "Rounded" };
        var panelBorderStyles = new[] { "None", "Subtle", "Strong" };
        var panelBorderColors = new[] { "White", "Black", "Theme" };
        var panelBorderThicknesses = new[] { "Thin", "Medium", "Thick" };
        var panelBorderOpacities = new[] { "Subtle", "Medium", "Strong" };
        var panelBorderOpacityPicker = new Picker
        {
            Title = _english ? "Panel border opacity" : "Doorzichtigheid rand tekstachtergrond",
            ItemsSource = panelBorderOpacities,
            SelectedItem = panelBorderOpacities.Contains(project.ThumbnailTextPanelBorderOpacity) ? project.ThumbnailTextPanelBorderOpacity : "Strong",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelBorderColorPicker = new Picker
        {
            Title = _english ? "Panel border color" : "Kleur rand tekstachtergrond",
            ItemsSource = panelBorderColors,
            SelectedItem = panelBorderColors.Contains(project.ThumbnailTextPanelBorderColor) ? project.ThumbnailTextPanelBorderColor : "White",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelBorderThicknessPicker = new Picker
        {
            Title = _english ? "Panel border thickness" : "Dikte rand tekstachtergrond",
            ItemsSource = panelBorderThicknesses,
            SelectedItem = panelBorderThicknesses.Contains(project.ThumbnailTextPanelBorderThickness) ? project.ThumbnailTextPanelBorderThickness : "Medium",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelBorderPicker = new Picker
        {
            Title = _english ? "Panel border" : "Rand tekstachtergrond",
            ItemsSource = panelBorderStyles,
            SelectedItem = panelBorderStyles.Contains(project.ThumbnailTextPanelBorder) ? project.ThumbnailTextPanelBorder : "None",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelCornersPicker = new Picker
        {
            Title = _english ? "Panel corners" : "Hoeken tekstachtergrond",
            ItemsSource = panelCornerStyles,
            SelectedItem = panelCornerStyles.Contains(project.ThumbnailTextPanelCorners) ? project.ThumbnailTextPanelCorners : "Rounded",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelPaddingPicker = new Picker
        {
            Title = _english ? "Panel padding" : "Ruimte rond tekst",
            ItemsSource = panelPaddings,
            SelectedItem = panelPaddings.Contains(project.ThumbnailTextPanelPadding) ? project.ThumbnailTextPanelPadding : "Normal",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelOpacityPicker = new Picker
        {
            Title = _english ? "Panel opacity" : "Doorzichtigheid achtergrond",
            ItemsSource = panelOpacities,
            SelectedItem = panelOpacities.Contains(project.ThumbnailTextPanelOpacity) ? project.ThumbnailTextPanelOpacity : "Medium",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var titlePlacements = new[] { "Top", "Center", "Bottom" };
        var titlePlacementPicker = new Picker
        {
            Title = _english ? "Text placement" : "Tekstpositie",
            ItemsSource = titlePlacements,
            SelectedItem = titlePlacements.Contains(project.ThumbnailTextPlacement) ? project.ThumbnailTextPlacement : "Center",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var panelStylePicker = new Picker
        {
            Title = _english ? "Text background panel" : "Achtergrond achter tekst",
            ItemsSource = panelStyles,
            SelectedItem = panelStyles.Contains(project.ThumbnailTextPanelStyle) ? project.ThumbnailTextPanelStyle : "Dark",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var presetNames = _english
            ? new[] { "Gaming impact", "Podcast", "Minimal", "Breaking news", "Cinematic" }
            : new[] { "Gaming-impact", "Podcast", "Minimalistisch", "Breaking news", "Filmisch" };
        var presetPicker = new Picker
        {
            Title = _english ? "Quick design preset" : "Snelontwerp kiezen",
            ItemsSource = presetNames,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var imageFitOptions = _english
            ? new[] { "Fill frame (crop)", "Fit full image" }
            : new[] { "Beeldvullend (uitsnijden)", "Hele afbeelding tonen" };
        var imageFitPicker = new Picker
        {
            Title = _english ? "Image framing" : "Beeldkader",
            ItemsSource = imageFitOptions,
            SelectedIndex = project.ThumbnailImageFit == "Fit" ? 1 : 0,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var layouts = new[] { "Center", "Bottom banner", "Top banner" };
        var layoutPicker = new Picker
        {
            Title = _english ? "Layout" : "Indeling",
            ItemsSource = layouts,
            SelectedItem = layouts.Contains(project.ThumbnailLayout) ? project.ThumbnailLayout : "Center",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#262638")
        };
        var previewHeadline = new Label
        {
            Text = titleEntry.Text,
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        var previewSubtitle = new Label
        {
            Text = subtitleEntry.Text,
            FontSize = 14,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        var previewImage = new Image
        {
            Aspect = Aspect.AspectFill,
            IsVisible = !string.IsNullOrWhiteSpace(project.ThumbnailImagePath) && File.Exists(project.ThumbnailImagePath)
        };
        if (previewImage.IsVisible)
            previewImage.Source = ImageSource.FromFile(project.ThumbnailImagePath!);
        var previewTextPanel = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Spacing = 10,
                Children = { previewHeadline, previewSubtitle }
            }
        };
        var safeAreaGuide = new Border
        {
            Margin = new Thickness(20),
            Stroke = Color.FromArgb("#FFFF00"),
            StrokeThickness = 1,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true,
            IsVisible = false
        };
        var safeAreaButton = new Button
        {
            Text = _english ? "Show safe-area guide" : "Veilige marge tonen",
            BackgroundColor = Color.FromArgb("#59451D"),
            TextColor = Colors.White,
            CornerRadius = 10
        };
        safeAreaButton.Clicked += (_, _) =>
        {
            safeAreaGuide.IsVisible = !safeAreaGuide.IsVisible;
            safeAreaButton.Text = safeAreaGuide.IsVisible
                ? (_english ? "Hide safe-area guide" : "Veilige marge verbergen")
                : (_english ? "Show safe-area guide" : "Veilige marge tonen");
        };
        var preview = new Border
        {
            HeightRequest = 190,
            Stroke = Color.FromArgb("#FFFFFF"),
            StrokeThickness = 1,
            Padding = 18,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Content = new Grid
            {
                Children =
                {
                    previewImage,
                    previewTextPanel,
                    safeAreaGuide
                }
            }
        };
        void RefreshPreview()
        {
            previewHeadline.Text = string.IsNullOrWhiteSpace(titleEntry.Text) ? (_english ? "Your headline" : "Jouw koptekst") : titleEntry.Text;
            previewSubtitle.Text = subtitleEntry.Text ?? string.Empty;
            previewImage.Aspect = imageFitPicker.SelectedIndex == 1 ? Aspect.AspectFit : Aspect.AspectFill;
            var textAlignment = alignmentPicker.SelectedItem?.ToString() ?? "Center";
            previewHeadline.HorizontalTextAlignment = textAlignment switch
            {
                "Left" => TextAlignment.Start,
                "Right" => TextAlignment.End,
                _ => TextAlignment.Center
            };
            previewSubtitle.HorizontalTextAlignment = previewHeadline.HorizontalTextAlignment;
            previewHeadline.FontFamily = fontFamilyPicker.SelectedItem?.ToString() ?? "Arial";
            previewSubtitle.FontFamily = fontFamilyPicker.SelectedItem?.ToString() ?? "Arial";
            previewHeadline.FontSize = titleSizePicker.SelectedItem?.ToString() switch { "Small" => 18, "Large" => 30, _ => 24 };
            previewSubtitle.FontSize = subtitleSizePicker.SelectedItem?.ToString() switch { "Small" => 11, "Large" => 18, _ => 14 };
            var textColor = textColorPicker.SelectedItem?.ToString() ?? "White";
            var previewColor = textColor switch { "Yellow" => Colors.Yellow, "Black" => Colors.Black, _ => Colors.White };
            previewHeadline.TextColor = previewColor;
            previewSubtitle.TextColor = previewColor;
            var outlineColor = outlineColorPicker.SelectedItem?.ToString() ?? "Black";
            var previewOutline = outlineColor switch { "White" => Colors.White, "Black" => Colors.Black, _ => Colors.Transparent };
            var outlineThickness = outlineThicknessPicker.SelectedItem?.ToString() ?? "Medium";
            var shadowRadius = outlineThickness switch { "Thin" => 1f, "Thick" => 5f, _ => 3f };
            previewHeadline.Shadow = new Shadow { Brush = previewOutline, Offset = new Point(1, 1), Radius = outlineColor == "None" ? 0 : shadowRadius, Opacity = outlineColor == "None" ? 0 : 1 };
            previewSubtitle.Shadow = new Shadow { Brush = previewOutline, Offset = new Point(1, 1), Radius = outlineColor == "None" ? 0 : shadowRadius, Opacity = outlineColor == "None" ? 0 : 1 };
            var layout = layoutPicker.SelectedItem?.ToString() ?? "Center";
            var placement = titlePlacementPicker.SelectedItem?.ToString() ?? "Center";
            var previewStack = (VerticalStackLayout)previewTextPanel.Content;
            previewStack.VerticalOptions = layout switch
            {
                "Top banner" => LayoutOptions.Start,
                "Bottom banner" => LayoutOptions.End,
                _ => LayoutOptions.Center
            };
            previewStack.Padding = new Thickness(0);
            previewTextPanel.VerticalOptions = placement switch
            {
                "Top" => LayoutOptions.Start,
                "Bottom" => LayoutOptions.End,
                _ => layout switch
                {
                    "Top banner" => LayoutOptions.Start,
                    "Bottom banner" => LayoutOptions.End,
                    _ => LayoutOptions.Center
                }
            };
            var panelStyle = panelStylePicker.SelectedItem?.ToString() ?? "Dark";
            previewTextPanel.IsVisible = panelStyle != "None";
            var panelAlpha = panelOpacityPicker.SelectedItem?.ToString() switch { "Subtle" => "66", "Strong" => "E6", _ => "B3" };
            previewTextPanel.Padding = panelPaddingPicker.SelectedItem?.ToString() switch { "Compact" => new Thickness(8, 5), "Spacious" => new Thickness(20, 16), _ => new Thickness(12, 10) };
            var cornerRadius = panelCornersPicker.SelectedItem?.ToString() switch { "Square" => 0, "Soft" => 6, _ => 12 };
            previewTextPanel.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = cornerRadius };
            var panelBorder = panelBorderPicker.SelectedItem?.ToString() ?? "None";
            var panelBorderThickness = panelBorderThicknessPicker.SelectedItem?.ToString() ?? "Medium";
            previewTextPanel.StrokeThickness = panelBorder == "None" ? 0 : panelBorderThickness switch { "Thin" => 1, "Thick" => 3, _ => 2 };
            var panelBorderColor = panelBorderColorPicker.SelectedItem?.ToString() ?? "White";
            var borderAlpha = panelBorderOpacityPicker.SelectedItem?.ToString() switch { "Subtle" => "66", "Medium" => "B3", _ => "FF" };
            var themeBorderColor = (picker.SelectedItem?.ToString() ?? "Purple") switch { "Blue" => Color.FromArgb("#60A5FA"), "Orange" => Color.FromArgb("#FDBA74"), "Green" => Color.FromArgb("#86EFAC"), _ => Color.FromArgb("#C4B5FD") };
            var selectedBorderColor = panelBorderColor == "Black" ? Colors.Black : panelBorderColor == "Theme" ? themeBorderColor : Colors.White;
            previewTextPanel.Stroke = panelBorder == "None" ? Colors.Transparent : Color.FromRgba(selectedBorderColor.Red, selectedBorderColor.Green, selectedBorderColor.Blue, (byte)Convert.ToInt32(borderAlpha, 16));
            previewTextPanel.BackgroundColor = panelStyle switch
            {
                "Light" => Color.FromArgb("#"+panelAlpha+"E5EAF5"),
                "Theme" => Color.FromArgb("#"+panelAlpha+(picker.SelectedItem?.ToString() ?? "Purple") switch { "Blue" => "155EAA", "Orange" => "C2410C", "Green" => "166534", _ => "6D28D9" }),
                _ => Color.FromArgb("#"+panelAlpha+"0C0C18")
            };
            var theme = picker.SelectedItem?.ToString() ?? "Purple";
            preview.BackgroundColor = Color.FromArgb(theme switch
            {
                "Blue" => "#155EAA",
                "Orange" => "#C2410C",
                "Green" => "#166534",
                _ => "#6D28D9"
            });
        }
        imageFitPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        presetPicker.SelectedIndexChanged += (_, _) =>
        {
            var preset = presetPicker.SelectedItem?.ToString() ?? string.Empty;
            var gaming = preset == (_english ? "Gaming impact" : "Gaming-impact");
            var podcast = preset == "Podcast";
            var minimal = preset == (_english ? "Minimal" : "Minimalistisch");
            var news = preset == "Breaking news";
            var cinematic = preset == (_english ? "Cinematic" : "Filmisch");

            picker.SelectedItem = gaming ? "Purple" : podcast ? "Blue" : minimal ? "Green" : news ? "Orange" : "Purple";
            alignmentPicker.SelectedItem = minimal ? "Left" : "Center";
            titleSizePicker.SelectedItem = gaming || news ? "Large" : minimal ? "Medium" : "Large";
            subtitleSizePicker.SelectedItem = minimal ? "Small" : "Medium";
            fontFamilyPicker.SelectedItem = cinematic ? "Georgia" : gaming || news ? "Arial" : podcast ? "Georgia" : "Arial";
            layoutPicker.SelectedItem = news || cinematic ? "Bottom banner" : minimal ? "Top banner" : "Center";
            textColorPicker.SelectedItem = minimal ? "Black" : news ? "Yellow" : "White";
            outlineColorPicker.SelectedItem = minimal ? "None" : "Black";
            outlineThicknessPicker.SelectedItem = "Medium";
            panelStylePicker.SelectedItem = minimal ? "Light" : podcast ? "Theme" : "Dark";
            panelOpacityPicker.SelectedItem = gaming || news ? "Strong" : "Medium";
            panelPaddingPicker.SelectedItem = minimal ? "Compact" : "Spacious";
            panelCornersPicker.SelectedItem = minimal || news ? "Square" : "Rounded";
            panelBorderPicker.SelectedItem = podcast ? "Subtle" : "None";
            panelBorderThicknessPicker.SelectedItem = "Medium";
            panelBorderOpacityPicker.SelectedItem = "Strong";
            panelBorderColorPicker.SelectedItem = "Theme";
            titlePlacementPicker.SelectedItem = news || cinematic ? "Bottom" : minimal ? "Top" : "Center";
            RefreshPreview();
        };
        titleEntry.TextChanged += (_, _) => RefreshPreview();
        subtitleEntry.TextChanged += (_, _) => RefreshPreview();
        picker.SelectedIndexChanged += (_, _) => RefreshPreview();
        alignmentPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        titleSizePicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        subtitleSizePicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        fontFamilyPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        layoutPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        textColorPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        outlineColorPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        outlineThicknessPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelStylePicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelOpacityPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelPaddingPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelCornersPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelBorderPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelBorderThicknessPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelBorderOpacityPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        panelBorderColorPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        titlePlacementPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        RefreshPreview();

        string? selectedImagePath = project.ThumbnailImagePath;
        var chooseImageButton = new Button
        {
            Text = _english ? "Choose thumbnail image" : "Thumbnail-afbeelding kiezen",
            BackgroundColor = Color.FromArgb("#262638"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        chooseImageButton.Clicked += async (_, _) =>
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = _english ? "Choose an image" : "Kies een afbeelding",
                    FileTypes = FilePickerFileType.Images
                });
                if (file is null) return;
                var folder = Path.Combine(FileSystem.AppDataDirectory, "CreatorNexusMedia");
                Directory.CreateDirectory(folder);
                var safeName = Path.GetFileName(file.FileName);
                if (string.IsNullOrWhiteSpace(safeName)) safeName = "thumbnail-image";
                var destination = Path.Combine(folder, $"{Guid.NewGuid():N}_{safeName}");
                await using (var input = await file.OpenReadAsync())
                await using (var output = File.Create(destination))
                    await input.CopyToAsync(output);
                selectedImagePath = destination;
                previewImage.Source = ImageSource.FromFile(destination);
                previewImage.IsVisible = true;
                preview.BackgroundColor = Color.FromArgb("#262638");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Image could not be added" : "Afbeelding toevoegen mislukt", ex.Message, _english ? "OK" : "Oké");
            }
        };
        var exportButton = new Button
        {
            Text = _english ? "Export PNG and share" : "Exporteer PNG en delen",
            BackgroundColor = Color.FromArgb("#155EAA"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var exportJpegButton = new Button
        {
            Text = _english ? "Export JPG and share" : "Exporteer JPG en delen",
            BackgroundColor = Color.FromArgb("#166534"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var save = new Button { Text = _english ? "Save design" : "Ontwerp opslaan", BackgroundColor = Color.FromArgb("#7C3AED"), TextColor = Colors.White, CornerRadius = 12 };
        var reuseDesignButton = new Button
        {
            Text = _english ? "Reuse design in another project" : "Ontwerp hergebruiken in ander project",
            BackgroundColor = Color.FromArgb("#155EAA"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var saveStyleButton = new Button
        {
            Text = _english ? "Save reusable style" : "Stijl opslaan",
            BackgroundColor = Color.FromArgb("#166534"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var loadStyleButton = new Button
        {
            Text = _english ? "Load saved style" : "Opgeslagen stijl laden",
            BackgroundColor = Color.FromArgb("#59451D"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var deleteStyleButton = new Button
        {
            Text = _english ? "Delete saved style" : "Opgeslagen stijl verwijderen",
            BackgroundColor = Color.FromArgb("#7F1D1D"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var duplicateStyleButton = new Button
        {
            Text = _english ? "Duplicate saved style" : "Opgeslagen stijl kopiëren",
            BackgroundColor = Color.FromArgb("#4C3B82"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var exportStyleButton = new Button
        {
            Text = _english ? "Export saved style" : "Opgeslagen stijl exporteren",
            BackgroundColor = Color.FromArgb("#155E75"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var importStyleButton = new Button
        {
            Text = _english ? "Import style file" : "Stijlbestand importeren",
            BackgroundColor = Color.FromArgb("#365314"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var exportStyleLibraryButton = new Button
        {
            Text = _english ? "Export all styles" : "Alle stijlen exporteren",
            BackgroundColor = Color.FromArgb("#155E75"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var importStyleLibraryButton = new Button
        {
            Text = _english ? "Import style library" : "Stijlenbibliotheek importeren",
            BackgroundColor = Color.FromArgb("#365314"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var cancel = new Button { Text = _english ? "Cancel" : "Annuleren", BackgroundColor = Color.FromArgb("#262638"), TextColor = Colors.White, CornerRadius = 12 };
        var buttons = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children = { cancel, save }
        };
        Grid.SetColumn(cancel, 0);
        Grid.SetColumn(save, 1);
        var page = new ContentPage
        {
            Title = _english ? "Thumbnail designer" : "Thumbnail-ontwerper",
            BackgroundColor = Color.FromArgb("#11111B"),
            Padding = new Thickness(18),
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    Children =
                    {
                        new Label { Text = _english ? "Live thumbnail preview" : "Live thumbnailvoorbeeld", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        preview,
                        safeAreaButton,
                        new Label { Text = _english ? "Headline" : "Koptekst", TextColor = Colors.White },
                        titleEntry,
                        new Label { Text = _english ? "Subtitle" : "Ondertitel", TextColor = Colors.White },
                        subtitleEntry,
                        new Label { Text = _english ? "Color theme" : "Kleurthema", TextColor = Colors.White },
                        picker,
                        new Label { Text = _english ? "Text alignment" : "Tekstuitlijning", TextColor = Colors.White },
                        alignmentPicker,
                        new Label { Text = _english ? "Headline size" : "Grootte koptekst", TextColor = Colors.White },
                        titleSizePicker,
                        new Label { Text = _english ? "Subtitle size" : "Grootte ondertitel", TextColor = Colors.White },
                        subtitleSizePicker,
                        new Label { Text = _english ? "Font family" : "Lettertype", TextColor = Colors.White },
                        fontFamilyPicker,
                        new Label { Text = _english ? "Quick design preset" : "Snelontwerp kiezen", TextColor = Colors.White },
                        presetPicker,
                        new Label { Text = _english ? "Image framing" : "Beeldkader", TextColor = Colors.White },
                        imageFitPicker,
                        new Label { Text = _english ? "Layout" : "Indeling", TextColor = Colors.White },
                        layoutPicker,
                        new Label { Text = _english ? "Text color" : "Tekstkleur", TextColor = Colors.White },
                        textColorPicker,
                        new Label { Text = _english ? "Text outline" : "Tekstomlijning", TextColor = Colors.White },
                        outlineColorPicker,
                        new Label { Text = _english ? "Outline thickness" : "Dikte tekstomlijning", TextColor = Colors.White },
                        outlineThicknessPicker,
                        new Label { Text = _english ? "Text background panel" : "Achtergrond achter tekst", TextColor = Colors.White },
                        panelStylePicker,
                        new Label { Text = _english ? "Panel opacity" : "Doorzichtigheid achtergrond", TextColor = Colors.White },
                        panelOpacityPicker,
                        new Label { Text = _english ? "Panel padding" : "Ruimte rond tekst", TextColor = Colors.White },
                        panelPaddingPicker,
                        new Label { Text = _english ? "Panel corners" : "Hoeken tekstachtergrond", TextColor = Colors.White },
                        panelCornersPicker,
                        panelBorderPicker,
                        panelBorderThicknessPicker,
                        panelBorderOpacityPicker,
                        panelBorderColorPicker,
                        new Label { Text = _english ? "Text placement" : "Tekstpositie", TextColor = Colors.White },
                        titlePlacementPicker,
                        chooseImageButton,
                        reuseDesignButton,
                        saveStyleButton,
                        loadStyleButton,
                        deleteStyleButton,
                        duplicateStyleButton,
                        exportStyleButton,
                        importStyleButton,
                        exportStyleLibraryButton,
                        importStyleLibraryButton,
                        exportButton,
                        exportJpegButton,
                        new Label { Text = _english ? "Optional: add your own image. Export creates a 1280×720 PNG or JPG." : "Optioneel: voeg je eigen afbeelding toe. Export maakt een PNG of JPG van 1280×720.", FontSize = 12, TextColor = Color.FromArgb("#B9B9C9") },
                        buttons
                    }
                }
            }
        };

        var completion = new TaskCompletionSource<bool>();
        exportButton.Clicked += async (_, _) =>
        {
            var headline = string.IsNullOrWhiteSpace(titleEntry.Text)
                ? (_english ? "Your headline" : "Jouw koptekst")
                : titleEntry.Text.Trim();
            var subtitle = subtitleEntry.Text?.Trim() ?? string.Empty;
            var theme = picker.SelectedItem?.ToString() ?? "Purple";
            try
            {
                var exportPath = await ExportThumbnailPngAsync(project.Title, headline, subtitle, theme, selectedImagePath, alignmentPicker.SelectedItem?.ToString() ?? "Center", titleSizePicker.SelectedItem?.ToString() ?? "Medium", subtitleSizePicker.SelectedItem?.ToString() ?? "Medium", layoutPicker.SelectedItem?.ToString() ?? "Center", textColorPicker.SelectedItem?.ToString() ?? "White", outlineColorPicker.SelectedItem?.ToString() ?? "Black", panelStylePicker.SelectedItem?.ToString() ?? "Dark", titlePlacementPicker.SelectedItem?.ToString() ?? "Center", outlineThicknessPicker.SelectedItem?.ToString() ?? "Medium", fontFamilyPicker.SelectedItem?.ToString() ?? "Arial", panelOpacityPicker.SelectedItem?.ToString() ?? "Medium", panelPaddingPicker.SelectedItem?.ToString() ?? "Normal", panelCornersPicker.SelectedItem?.ToString() ?? "Rounded", panelBorderPicker.SelectedItem?.ToString() ?? "None", panelBorderColorPicker.SelectedItem?.ToString() ?? "White", panelBorderThicknessPicker.SelectedItem?.ToString() ?? "Medium", panelBorderOpacityPicker.SelectedItem?.ToString() ?? "Strong", false, imageFitPicker.SelectedIndex == 1 ? "Fit" : "Cover");
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = _english ? "Share thumbnail PNG" : "Thumbnail-PNG delen",
                    File = new ShareFile(exportPath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Export failed" : "Exporteren mislukt", ex.Message, _english ? "OK" : "Oké");
            }
        };
        exportJpegButton.Clicked += async (_, _) =>
        {
            var headline = string.IsNullOrWhiteSpace(titleEntry.Text)
                ? (_english ? "Your headline" : "Jouw koptekst")
                : titleEntry.Text.Trim();
            var subtitle = subtitleEntry.Text?.Trim() ?? string.Empty;
            var theme = picker.SelectedItem?.ToString() ?? "Purple";
            try
            {
                var exportPath = await ExportThumbnailPngAsync(project.Title, headline, subtitle, theme, selectedImagePath, alignmentPicker.SelectedItem?.ToString() ?? "Center", titleSizePicker.SelectedItem?.ToString() ?? "Medium", subtitleSizePicker.SelectedItem?.ToString() ?? "Medium", layoutPicker.SelectedItem?.ToString() ?? "Center", textColorPicker.SelectedItem?.ToString() ?? "White", outlineColorPicker.SelectedItem?.ToString() ?? "Black", panelStylePicker.SelectedItem?.ToString() ?? "Dark", titlePlacementPicker.SelectedItem?.ToString() ?? "Center", outlineThicknessPicker.SelectedItem?.ToString() ?? "Medium", fontFamilyPicker.SelectedItem?.ToString() ?? "Arial", panelOpacityPicker.SelectedItem?.ToString() ?? "Medium", panelPaddingPicker.SelectedItem?.ToString() ?? "Normal", panelCornersPicker.SelectedItem?.ToString() ?? "Rounded", panelBorderPicker.SelectedItem?.ToString() ?? "None", panelBorderColorPicker.SelectedItem?.ToString() ?? "White", panelBorderThicknessPicker.SelectedItem?.ToString() ?? "Medium", panelBorderOpacityPicker.SelectedItem?.ToString() ?? "Strong", true, imageFitPicker.SelectedIndex == 1 ? "Fit" : "Cover");
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = _english ? "Share thumbnail JPG" : "Thumbnail-JPG delen",
                    File = new ShareFile(exportPath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Export failed" : "Exporteren mislukt", ex.Message, _english ? "OK" : "Oké");
            }
        };
        ThumbnailStylePreset BuildStylePreset(string name) => new()
        {
            Name = name,
            Theme = picker.SelectedItem?.ToString() ?? "Purple",
            TextAlignment = alignmentPicker.SelectedItem?.ToString() ?? "Center",
            TitleSize = titleSizePicker.SelectedItem?.ToString() ?? "Medium",
            SubtitleSize = subtitleSizePicker.SelectedItem?.ToString() ?? "Medium",
            FontFamily = fontFamilyPicker.SelectedItem?.ToString() ?? "Arial",
            Layout = layoutPicker.SelectedItem?.ToString() ?? "Center",
            TextColor = textColorPicker.SelectedItem?.ToString() ?? "White",
            TextOutlineColor = outlineColorPicker.SelectedItem?.ToString() ?? "Black",
            TextOutlineThickness = outlineThicknessPicker.SelectedItem?.ToString() ?? "Medium",
            TextPanelStyle = panelStylePicker.SelectedItem?.ToString() ?? "Dark",
            TextPanelOpacity = panelOpacityPicker.SelectedItem?.ToString() ?? "Medium",
            TextPanelPadding = panelPaddingPicker.SelectedItem?.ToString() ?? "Normal",
            TextPanelCorners = panelCornersPicker.SelectedItem?.ToString() ?? "Rounded",
            TextPanelBorder = panelBorderPicker.SelectedItem?.ToString() ?? "None",
            TextPanelBorderColor = panelBorderColorPicker.SelectedItem?.ToString() ?? "White",
            TextPanelBorderThickness = panelBorderThicknessPicker.SelectedItem?.ToString() ?? "Medium",
            TextPanelBorderOpacity = panelBorderOpacityPicker.SelectedItem?.ToString() ?? "Strong",
            TextPlacement = titlePlacementPicker.SelectedItem?.ToString() ?? "Center",
            ImageFit = imageFitPicker.SelectedIndex == 1 ? "Fit" : "Cover"
        };
        saveStyleButton.Clicked += async (_, _) =>
        {
            var defaultName = $"{(picker.SelectedItem?.ToString() ?? "Purple")} {(_english ? "style" : "stijl")}";
            var name = await DisplayPromptAsync(
                _english ? "Save thumbnail style" : "Thumbnailstijl opslaan",
                _english ? "Give this reusable style a name." : "Geef deze herbruikbare stijl een naam.",
                initialValue: defaultName,
                maxLength: 50);
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                var trimmedName = name.Trim();
                var existingStyles = await styleStore.LoadAsync();
                var existingStyle = existingStyles.FirstOrDefault(x =>
                    string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
                if (existingStyle is not null)
                {
                    var replace = await DisplayAlertAsync(
                        _english ? "Replace saved style?" : "Opgeslagen stijl vervangen?",
                        _english
                            ? $"A style named “{existingStyle.Name}” already exists. Replace it with the current settings?"
                            : $"Er bestaat al een stijl met de naam “{existingStyle.Name}”. Wil je deze vervangen door de huidige instellingen?",
                        _english ? "Replace" : "Vervangen",
                        _english ? "Cancel" : "Annuleren");
                    if (!replace) return;
                }

                await styleStore.SaveAsync(BuildStylePreset(trimmedName));
                await DisplayAlertAsync(
                    _english ? "Style saved" : "Stijl opgeslagen",
                    _english ? $"“{trimmedName}” is available in your saved styles." : $"“{trimmedName}” staat nu bij je opgeslagen stijlen.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Could not save style" : "Stijl opslaan mislukt", ex.Message, "OK");
            }
        };
        deleteStyleButton.Clicked += async (_, _) =>
        {
            try
            {
                var styles = await styleStore.LoadAsync();
                if (styles.Count == 0)
                {
                    await DisplayAlertAsync(
                        _english ? "No saved styles" : "Geen opgeslagen stijlen",
                        _english ? "There are no saved styles to delete." : "Er zijn geen opgeslagen stijlen om te verwijderen.",
                        "OK");
                    return;
                }

                var searchText = await DisplayPromptAsync(
                    _english ? "Find a style to delete" : "Zoek een stijl om te verwijderen",
                    _english ? "Enter part of the style name, or leave blank to show all styles." : "Typ een deel van de stijlnaam of laat leeg om alle stijlen te tonen.",
                    _english ? "Search" : "Zoeken",
                    _english ? "Show all" : "Alles tonen",
                    maxLength: 50);
                if (searchText is null) return;
                var matchingStyles = (string.IsNullOrWhiteSpace(searchText)
                    ? styles
                    : styles.Where(x => x.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matchingStyles.Count == 0)
                {
                    await DisplayAlertAsync(_english ? "No matching styles" : "Geen stijlen gevonden",
                        _english ? "Try another name or search term." : "Probeer een andere naam of zoekterm.", "OK");
                    return;
                }
                var selectedName = await DisplayActionSheetAsync(
                    _english ? "Choose a style to delete" : "Kies een stijl om te verwijderen",
                    _english ? "Cancel" : "Annuleren",
                    null,
                    matchingStyles.Select(x => x.Name).ToArray());
                if (string.IsNullOrWhiteSpace(selectedName) || selectedName == (_english ? "Cancel" : "Annuleren"))
                    return;

                var confirmed = await DisplayAlertAsync(
                    _english ? "Delete saved style?" : "Opgeslagen stijl verwijderen?",
                    _english ? $"Delete “{selectedName}”? This cannot be undone." : $"“{selectedName}” verwijderen? Dit kan niet ongedaan worden gemaakt.",
                    _english ? "Delete" : "Verwijderen",
                    _english ? "Keep" : "Behouden");
                if (!confirmed) return;

                var deleted = await styleStore.DeleteAsync(selectedName);
                await DisplayAlertAsync(
                    _english ? (deleted ? "Style deleted" : "Style not found") : (deleted ? "Stijl verwijderd" : "Stijl niet gevonden"),
                    _english ? (deleted ? $"“{selectedName}” was removed from your style library." : "The style could not be found.") : (deleted ? $"“{selectedName}” is uit je stijlenbibliotheek verwijderd." : "De stijl kon niet worden gevonden."),
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Could not delete style" : "Stijl verwijderen mislukt", ex.Message, "OK");
            }
        };
        duplicateStyleButton.Clicked += async (_, _) =>
        {
            try
            {
                var styles = await styleStore.LoadAsync();
                if (styles.Count == 0)
                {
                    await DisplayAlertAsync(
                        _english ? "No saved styles" : "Geen opgeslagen stijlen",
                        _english ? "Save a style first, then you can duplicate it." : "Sla eerst een stijl op; daarna kun je deze kopiëren.",
                        "OK");
                    return;
                }

                var searchText = await DisplayPromptAsync(
                    _english ? "Find a style to duplicate" : "Zoek een stijl om te kopiëren",
                    _english ? "Enter part of the style name, or leave blank to show all styles." : "Typ een deel van de stijlnaam of laat leeg om alle stijlen te tonen.",
                    _english ? "Search" : "Zoeken",
                    _english ? "Show all" : "Alles tonen",
                    maxLength: 50);
                if (searchText is null) return;
                var matchingStyles = (string.IsNullOrWhiteSpace(searchText)
                    ? styles
                    : styles.Where(x => x.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matchingStyles.Count == 0)
                {
                    await DisplayAlertAsync(_english ? "No matching styles" : "Geen stijlen gevonden",
                        _english ? "Try another name or search term." : "Probeer een andere naam of zoekterm.", "OK");
                    return;
                }
                var selectedName = await DisplayActionSheetAsync(
                    _english ? "Choose a style to duplicate" : "Kies een stijl om te kopiëren",
                    _english ? "Cancel" : "Annuleren",
                    null,
                    matchingStyles.Select(x => x.Name).ToArray());
                if (string.IsNullOrWhiteSpace(selectedName) || selectedName == (_english ? "Cancel" : "Annuleren"))
                    return;

                var selected = styles.FirstOrDefault(x => string.Equals(x.Name, selectedName, StringComparison.OrdinalIgnoreCase));
                if (selected is null) return;

                var newName = await DisplayPromptAsync(
                    _english ? "Duplicate style" : "Stijl kopiëren",
                    _english ? "Choose a new name for the copy." : "Geef de kopie een nieuwe naam.",
                    initialValue: $"{selected.Name} {( _english ? "copy" : "kopie")}",
                    maxLength: 50);
                if (string.IsNullOrWhiteSpace(newName)) return;
                var trimmedName = newName.Trim();
                if (styles.Any(x => string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
                {
                    await DisplayAlertAsync(
                        _english ? "Name already in use" : "Naam bestaat al",
                        _english
                            ? "A saved style already uses that name. Choose a different name so the existing style is not overwritten."
                            : "Er bestaat al een opgeslagen stijl met deze naam. Kies een andere naam zodat de bestaande stijl niet wordt overschreven.",
                        "OK");
                    return;
                }

                await styleStore.SaveAsync(selected with { Name = trimmedName });
                await DisplayAlertAsync(
                    _english ? "Style duplicated" : "Stijl gekopieerd",
                    _english ? $"A copy named “{trimmedName}” is now in your style library." : $"De kopie “{trimmedName}” staat nu in je stijlenbibliotheek.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Could not duplicate style" : "Stijl kopiëren mislukt", ex.Message, "OK");
            }
        };
        exportStyleButton.Clicked += async (_, _) =>
        {
            try
            {
                var styles = await styleStore.LoadAsync();
                if (styles.Count == 0)
                {
                    await DisplayAlertAsync(_english ? "No saved styles" : "Geen opgeslagen stijlen", _english ? "Save a style before exporting." : "Sla eerst een stijl op voordat je exporteert.", "OK");
                    return;
                }

                var searchText = await DisplayPromptAsync(
                    _english ? "Find a style to export" : "Zoek een stijl om te exporteren",
                    _english ? "Enter part of the style name, or leave blank to show all styles." : "Typ een deel van de stijlnaam of laat leeg om alle stijlen te tonen.",
                    _english ? "Search" : "Zoeken",
                    _english ? "Show all" : "Alles tonen",
                    maxLength: 50);
                if (searchText is null) return;
                var matchingStyles = (string.IsNullOrWhiteSpace(searchText)
                    ? styles
                    : styles.Where(x => x.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matchingStyles.Count == 0)
                {
                    await DisplayAlertAsync(_english ? "No matching styles" : "Geen stijlen gevonden",
                        _english ? "Try another name or search term." : "Probeer een andere naam of zoekterm.", "OK");
                    return;
                }
                var selectedName = await DisplayActionSheetAsync(
                    _english ? "Choose a style to export" : "Kies een stijl om te exporteren",
                    _english ? "Cancel" : "Annuleren", null, matchingStyles.Select(x => x.Name).ToArray());
                if (string.IsNullOrWhiteSpace(selectedName) || selectedName == (_english ? "Cancel" : "Annuleren")) return;
                var style = styles.FirstOrDefault(x => string.Equals(x.Name, selectedName, StringComparison.OrdinalIgnoreCase));
                if (style is null) return;

                var exportFolder = Path.Combine(FileSystem.AppDataDirectory, "StyleExports");
                Directory.CreateDirectory(exportFolder);
                var safeName = string.Concat(style.Name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
                if (string.IsNullOrWhiteSpace(safeName)) safeName = "thumbnail-style";
                var path = Path.Combine(exportFolder, safeName + ".creatornexus-style.json");
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(style, new JsonSerializerOptions { WriteIndented = true }));
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = _english ? "Share CreatorNexus style" : "CreatorNexus-stijl delen",
                    File = new ShareFile(path)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Style export failed" : "Stijl exporteren mislukt", ex.Message, "OK");
            }
        };
        exportStyleLibraryButton.Clicked += async (_, _) =>
        {
            try
            {
                var styles = await styleStore.LoadAsync();
                if (styles.Count == 0)
                {
                    await DisplayAlertAsync(_english ? "No saved styles" : "Geen opgeslagen stijlen",
                        _english ? "Save at least one style before exporting the library." : "Sla eerst minstens één stijl op om de bibliotheek te exporteren.", "OK");
                    return;
                }

                var exportFolder = Path.Combine(FileSystem.AppDataDirectory, "StyleExports");
                Directory.CreateDirectory(exportFolder);
                var path = Path.Combine(exportFolder, "CreatorNexus-style-library.json");
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(styles, new JsonSerializerOptions { WriteIndented = true }));
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = _english ? "Share CreatorNexus style library" : "CreatorNexus-stijlenbibliotheek delen",
                    File = new ShareFile(path)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Library export failed" : "Bibliotheek exporteren mislukt", ex.Message, "OK");
            }
        };
        importStyleLibraryButton.Clicked += async (_, _) =>
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = _english ? "Choose a CreatorNexus style library JSON file" : "Kies een JSON-bestand met CreatorNexus-stijlen",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = new[] { "application/json", "text/json" },
                        [DevicePlatform.WinUI] = new[] { ".json" },
                        [DevicePlatform.iOS] = new[] { "public.json" },
                        [DevicePlatform.MacCatalyst] = new[] { "public.json" }
                    })
                });
                if (file is null) return;
                await using var stream = await file.OpenReadAsync();
                var imported = await JsonSerializer.DeserializeAsync<List<ThumbnailStylePreset>>(stream);
                if (imported is null || imported.Count == 0 || imported.Any(x => x is null || !x.IsValid()))
                {
                    await DisplayAlertAsync(
                        _english ? "Invalid style library" : "Ongeldige stijlenbibliotheek",
                        _english ? "The file is empty or contains invalid styles. Nothing was imported." : "Het bestand is leeg of bevat ongeldige stijlen. Er is niets geïmporteerd.",
                        "OK");
                    return;
                }

                var duplicateNames = imported.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
                if (duplicateNames is not null)
                {
                    await DisplayAlertAsync(
                        _english ? "Duplicate names in file" : "Dubbele namen in bestand",
                        _english ? $"The file contains more than one style named “{duplicateNames.Key}”. Nothing was imported." : $"Het bestand bevat meerdere stijlen met de naam “{duplicateNames.Key}”. Er is niets geïmporteerd.",
                        "OK");
                    return;
                }

                var existing = await styleStore.LoadAsync();
                var collisions = imported.Where(x => existing.Any(y => string.Equals(y.Name, x.Name.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();
                var addedPreviewCount = imported.Count - collisions.Count;
                var replacedPreviewCount = collisions.Count;
                var collisionNames = collisions
                    .Select(style => style.Name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var collisionPreview = string.Join(", ", collisionNames.Take(5).Select(name => $"“{name}”"));
                if (collisionNames.Count > 5)
                {
                    collisionPreview += _english
                        ? $", and {collisionNames.Count - 5} more"
                        : $", en nog {collisionNames.Count - 5}";
                }

                var importSummary = _english
                    ? $"Ready to import {imported.Count} style(s): {addedPreviewCount} new and {replacedPreviewCount} replacement(s)."
                    : $"Klaar om {imported.Count} stijl(en) te importeren: {addedPreviewCount} nieuw en {replacedPreviewCount} vervanging(en).";
                if (collisionNames.Count > 0)
                {
                    importSummary += _english
                        ? $"\nStyles to replace: {collisionPreview}"
                        : $"\nTe vervangen stijlen: {collisionPreview}";
                }

                var confirmed = await DisplayAlertAsync(
                    _english ? "Confirm style import" : "Importeren bevestigen",
                    importSummary,
                    _english ? "Import" : "Importeren",
                    _english ? "Cancel" : "Annuleren");
                if (!confirmed) return;

                await styleStore.SaveManyAsync(imported.Select(style => style with { Name = style.Name.Trim() }));

                var addedCount = imported.Count - collisions.Count;
                var replacedCount = collisions.Count;
                var resultMessage = _english
                    ? $"Import complete: {addedCount} added, {replacedCount} replaced."
                    : $"Importeren voltooid: {addedCount} toegevoegd, {replacedCount} vervangen.";
                await DisplayAlertAsync(
                    _english ? "Style library imported" : "Stijlenbibliotheek geïmporteerd",
                    resultMessage,
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Library import failed" : "Bibliotheek importeren mislukt", ex.Message, "OK");
            }
        };
        importStyleButton.Clicked += async (_, _) =>
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = _english ? "Choose a CreatorNexus style JSON file" : "Kies een CreatorNexus-stijlbestand (JSON)",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = new[] { "application/json", "text/json" },
                        [DevicePlatform.WinUI] = new[] { ".json" },
                        [DevicePlatform.iOS] = new[] { "public.json" },
                        [DevicePlatform.MacCatalyst] = new[] { "public.json" }
                    })
                });
                if (file is null) return;
                await using var stream = await file.OpenReadAsync();
                var style = await JsonSerializer.DeserializeAsync<ThumbnailStylePreset>(stream);
                if (style is null || !style.IsValid())
                {
                    await DisplayAlertAsync(
                        _english ? "Invalid style file" : "Ongeldig stijlbestand",
                        _english
                            ? "The file is missing a valid name or contains unsupported design options. Nothing was imported."
                            : "Het bestand mist een geldige naam of bevat niet-ondersteunde ontwerpopties. Er is niets geïmporteerd.",
                        "OK");
                    return;
                }

                var name = style.Name.Trim();
                var overwrite = (await styleStore.LoadAsync()).Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (overwrite)
                {
                    var confirmed = await DisplayAlertAsync(
                        _english ? "Replace saved style?" : "Opgeslagen stijl vervangen?",
                        _english ? $"A style named “{name}” already exists. Replace it?" : $"Er bestaat al een stijl met de naam “{name}”. Wil je deze vervangen?",
                        _english ? "Replace" : "Vervangen",
                        _english ? "Cancel" : "Annuleren");
                    if (!confirmed) return;
                }

                await styleStore.SaveAsync(style with { Name = name });
                await DisplayAlertAsync(_english ? "Style imported" : "Stijl geïmporteerd",
                    _english ? $"“{name}” is now available in your saved styles." : $"“{name}” staat nu bij je opgeslagen stijlen.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Style import failed" : "Stijl importeren mislukt", ex.Message, "OK");
            }
        };
        loadStyleButton.Clicked += async (_, _) =>
        {
            try
            {
                var styles = await styleStore.LoadAsync();
                if (styles.Count == 0)
                {
                    await DisplayAlertAsync(
                        _english ? "No saved styles" : "Geen opgeslagen stijlen",
                        _english ? "Save a style first to build your reusable style library." : "Sla eerst een stijl op om je eigen stijlenbibliotheek op te bouwen.",
                        "OK");
                    return;
                }

                var searchText = await DisplayPromptAsync(
                    _english ? "Find a saved style" : "Zoek een opgeslagen stijl",
                    _english ? "Enter part of a style name, or leave blank to show all styles." : "Typ een deel van de stijlnaam of laat leeg om alle stijlen te tonen.",
                    _english ? "Search" : "Zoeken",
                    _english ? "Show all" : "Alles tonen",
                    maxLength: 50);
                if (searchText is null) return;

                var matchingStyles = (string.IsNullOrWhiteSpace(searchText)
                    ? styles
                    : styles.Where(x => x.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matchingStyles.Count == 0)
                {
                    await DisplayAlertAsync(
                        _english ? "No matching styles" : "Geen stijlen gevonden",
                        _english ? "Try another name or search term." : "Probeer een andere naam of zoekterm.",
                        "OK");
                    return;
                }

                var options = matchingStyles.Select(x => x.Name).ToArray();
                var selectedName = await DisplayActionSheetAsync(
                    _english ? "Choose a saved style" : "Kies een opgeslagen stijl",
                    _english ? "Cancel" : "Annuleren",
                    null,
                    options);
                if (string.IsNullOrWhiteSpace(selectedName) || selectedName == (_english ? "Cancel" : "Annuleren"))
                    return;

                var style = styles.FirstOrDefault(x => x.Name == selectedName);
                if (style is null) return;
                picker.SelectedItem = style.Theme;
                alignmentPicker.SelectedItem = style.TextAlignment;
                titleSizePicker.SelectedItem = style.TitleSize;
                subtitleSizePicker.SelectedItem = style.SubtitleSize;
                fontFamilyPicker.SelectedItem = style.FontFamily;
                layoutPicker.SelectedItem = style.Layout;
                textColorPicker.SelectedItem = style.TextColor;
                outlineColorPicker.SelectedItem = style.TextOutlineColor;
                outlineThicknessPicker.SelectedItem = style.TextOutlineThickness;
                panelStylePicker.SelectedItem = style.TextPanelStyle;
                panelOpacityPicker.SelectedItem = style.TextPanelOpacity;
                panelPaddingPicker.SelectedItem = style.TextPanelPadding;
                panelCornersPicker.SelectedItem = style.TextPanelCorners;
                panelBorderPicker.SelectedItem = style.TextPanelBorder;
                panelBorderColorPicker.SelectedItem = style.TextPanelBorderColor;
                panelBorderThicknessPicker.SelectedItem = style.TextPanelBorderThickness;
                panelBorderOpacityPicker.SelectedItem = style.TextPanelBorderOpacity;
                titlePlacementPicker.SelectedItem = style.TextPlacement;
                imageFitPicker.SelectedIndex = style.ImageFit == "Fit" ? 1 : 0;
                RefreshPreview();
                await DisplayAlertAsync(
                    _english ? "Style loaded" : "Stijl geladen",
                    _english ? $"“{style.Name}” has been applied. Save the project to keep the changes." : $"“{style.Name}” is toegepast. Sla het project op om de wijzigingen te bewaren.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_english ? "Could not load styles" : "Stijlen laden mislukt", ex.Message, "OK");
            }
        };

        reuseDesignButton.Clicked += async (_, _) =>
        {
            var headline = titleEntry.Text?.Trim() ?? string.Empty;
            var allProjects = await _store.LoadAsync();
            var targets = allProjects.Where(p => p.Id != project.Id && !p.IsArchived).ToList();
            if (targets.Count == 0)
            {
                await DisplayAlertAsync(
                    _english ? "No destination projects" : "Geen doelprojecten",
                    _english ? "Create another project first, then reuse this design." : "Maak eerst een ander project aan en hergebruik daarna dit ontwerp.",
                    "OK");
                return;
            }

            var options = targets.Select(p => p.Title).ToArray();
            var selectedTitle = await DisplayActionSheetAsync(
                _english ? "Apply design to which project?" : "Op welk project wil je dit ontwerp toepassen?",
                _english ? "Cancel" : "Annuleren",
                null,
                options);
            if (string.IsNullOrWhiteSpace(selectedTitle) ||
                selectedTitle == (_english ? "Cancel" : "Annuleren"))
                return;

            var target = targets.FirstOrDefault(p => p.Title == selectedTitle);
            if (target is null) return;

            var design = project with
            {
                ThumbnailTitle = headline,
                ThumbnailSubtitle = subtitleEntry.Text?.Trim() ?? string.Empty,
                ThumbnailTheme = picker.SelectedItem?.ToString() ?? "Purple",
                ThumbnailImagePath = selectedImagePath,
                ThumbnailImageFit = imageFitPicker.SelectedIndex == 1 ? "Fit" : "Cover",
                ThumbnailTextAlignment = alignmentPicker.SelectedItem?.ToString() ?? "Center",
                ThumbnailTitleSize = titleSizePicker.SelectedItem?.ToString() ?? "Medium",
                ThumbnailSubtitleSize = subtitleSizePicker.SelectedItem?.ToString() ?? "Medium",
                ThumbnailFontFamily = fontFamilyPicker.SelectedItem?.ToString() ?? "Arial",
                ThumbnailLayout = layoutPicker.SelectedItem?.ToString() ?? "Center",
                ThumbnailTextColor = textColorPicker.SelectedItem?.ToString() ?? "White",
                ThumbnailTextOutlineColor = outlineColorPicker.SelectedItem?.ToString() ?? "Black",
                ThumbnailTextOutlineThickness = outlineThicknessPicker.SelectedItem?.ToString() ?? "Medium",
                ThumbnailTextPanelStyle = panelStylePicker.SelectedItem?.ToString() ?? "Dark",
                ThumbnailTextPanelOpacity = panelOpacityPicker.SelectedItem?.ToString() ?? "Medium",
                ThumbnailTextPanelPadding = panelPaddingPicker.SelectedItem?.ToString() ?? "Normal",
                ThumbnailTextPanelCorners = panelCornersPicker.SelectedItem?.ToString() ?? "Rounded",
                ThumbnailTextPanelBorder = panelBorderPicker.SelectedItem?.ToString() ?? "None",
                ThumbnailTextPanelBorderColor = panelBorderColorPicker.SelectedItem?.ToString() ?? "White",
                ThumbnailTextPanelBorderThickness = panelBorderThicknessPicker.SelectedItem?.ToString() ?? "Medium",
                ThumbnailTextPanelBorderOpacity = panelBorderOpacityPicker.SelectedItem?.ToString() ?? "Strong",
                ThumbnailTextPlacement = titlePlacementPicker.SelectedItem?.ToString() ?? "Center"
            };
            await _store.UpdateAsync(CopyThumbnailDesign(design, target));
            await DisplayAlertAsync(
                _english ? "Design copied" : "Ontwerp overgenomen",
                _english ? $"The thumbnail design was copied to “{target.Title}”." : $"Het thumbnailontwerp is overgenomen in “{target.Title}”.",
                "OK");
        };
        save.Clicked += async (_, _) =>
        {
            await Navigation.PopModalAsync();
            completion.TrySetResult(true);
        };
        cancel.Clicked += async (_, _) =>
        {
            await Navigation.PopModalAsync();
            completion.TrySetResult(false);
        };
        await Navigation.PushModalAsync(page);
        if (!await completion.Task) return;

        await _store.UpdateAsync(project with
        {
            ThumbnailTitle = titleEntry.Text?.Trim() ?? string.Empty,
            ThumbnailSubtitle = subtitleEntry.Text?.Trim() ?? string.Empty,
            ThumbnailTheme = picker.SelectedItem?.ToString() ?? "Purple",
            ThumbnailImagePath = selectedImagePath,
            ThumbnailImageFit = imageFitPicker.SelectedIndex == 1 ? "Fit" : "Cover",
            ThumbnailTextAlignment = alignmentPicker.SelectedItem?.ToString() ?? "Center",
            ThumbnailTitleSize = titleSizePicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailSubtitleSize = subtitleSizePicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailFontFamily = fontFamilyPicker.SelectedItem?.ToString() ?? "Arial",
            ThumbnailLayout = layoutPicker.SelectedItem?.ToString() ?? "Center",
            ThumbnailTextColor = textColorPicker.SelectedItem?.ToString() ?? "White",
            ThumbnailTextOutlineColor = outlineColorPicker.SelectedItem?.ToString() ?? "Black",
            ThumbnailTextOutlineThickness = outlineThicknessPicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailTextPanelStyle = panelStylePicker.SelectedItem?.ToString() ?? "Dark",
            ThumbnailTextPanelOpacity = panelOpacityPicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailTextPanelPadding = panelPaddingPicker.SelectedItem?.ToString() ?? "Normal",
            ThumbnailTextPanelCorners = panelCornersPicker.SelectedItem?.ToString() ?? "Rounded",
            ThumbnailTextPanelBorder = panelBorderPicker.SelectedItem?.ToString() ?? "None",
            ThumbnailTextPanelBorderColor = panelBorderColorPicker.SelectedItem?.ToString() ?? "White",
            ThumbnailTextPanelBorderThickness = panelBorderThicknessPicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailTextPanelBorderOpacity = panelBorderOpacityPicker.SelectedItem?.ToString() ?? "Strong",
            ThumbnailTextPlacement = titlePlacementPicker.SelectedItem?.ToString() ?? "Center",
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await LoadProjectsAsync();
    }

    private async Task<string> ExportThumbnailPngAsync(string projectTitle, string headline, string subtitle, string theme, string? imagePath, string textAlignment = "Center", string titleSize = "Medium", string subtitleSize = "Medium", string layout = "Center", string textColor = "White", string outlineColor = "Black", string panelStyle = "Dark", string textPlacement = "Center", string outlineThickness = "Medium", string fontFamily = "Arial", string panelOpacity = "Medium", string panelPadding = "Normal", string panelCorners = "Rounded", string panelBorder = "None", string panelBorderColor = "White", string panelBorderThickness = "Medium", string panelBorderOpacity = "Strong", bool jpeg = false, string imageFit = "Cover")
    {
        var background = theme switch
        {
            "Blue" => SKColor.Parse("#155EAA"),
            "Orange" => SKColor.Parse("#C2410C"),
            "Green" => SKColor.Parse("#166534"),
            _ => SKColor.Parse("#6D28D9")
        };
        const int width = 1280;
        const int height = 720;
        using var bitmap = new SKBitmap(width, height);
        using var titleTypeface = SKTypeface.FromFamilyName(fontFamily, SKFontStyle.Bold);
        using var subtitleTypeface = SKTypeface.FromFamilyName(fontFamily, SKFontStyle.Normal);
        using var titleFont = new SKFont(titleTypeface, titleSize switch { "Small" => 60, "Large" => 92, _ => 76 });
        using var subtitleFont = new SKFont(subtitleTypeface, subtitleSize switch { "Small" => 26, "Large" => 42, _ => 34 });
        using (var canvas = new SKCanvas(bitmap))
        using (var panelPaint = new SKPaint { Color = new SKColor(12, 12, 24, 70), IsAntialias = true })
        using (var textPanelPaint = new SKPaint { Color = panelStyle switch { "Light" => new SKColor(230, 235, 245, (byte)(panelOpacity == "Subtle" ? 102 : panelOpacity == "Strong" ? 230 : 179)), "Theme" => new SKColor(background.Red, background.Green, background.Blue, (byte)(panelOpacity == "Subtle" ? 102 : panelOpacity == "Strong" ? 230 : 179)), "None" => SKColors.Transparent, _ => new SKColor(12, 12, 24, (byte)(panelOpacity == "Subtle" ? 102 : panelOpacity == "Strong" ? 230 : 179)) }, IsAntialias = true })
        using (var textPanelBorderPaint = new SKPaint { Color = (panelBorderColor switch { "Black" => SKColors.Black, "Theme" => background, _ => SKColors.White }).WithAlpha((byte)(panelBorderOpacity == "Subtle" ? 102 : panelBorderOpacity == "Medium" ? 179 : 255)), Style = SKPaintStyle.Stroke, StrokeWidth = panelBorderThickness switch { "Thin" => 2f, "Thick" => 7f, _ => 4f }, IsAntialias = true })
        using (var titleOutlinePaint = new SKPaint { Color = outlineColor switch { "White" => SKColors.White, "None" => SKColors.Transparent, _ => SKColors.Black }, Style = SKPaintStyle.Stroke, StrokeWidth = outlineThickness switch { "Thin" => 3, "Thick" => 11, _ => 7 }, IsAntialias = true, StrokeJoin = SKStrokeJoin.Round })
        using (var subtitleOutlinePaint = new SKPaint { Color = outlineColor switch { "White" => SKColors.White, "None" => SKColors.Transparent, _ => SKColors.Black }, Style = SKPaintStyle.Stroke, StrokeWidth = outlineThickness switch { "Thin" => 2, "Thick" => 8, _ => 5 }, IsAntialias = true, StrokeJoin = SKStrokeJoin.Round })
        using (var titlePaint = new SKPaint { Color = textColor switch { "Yellow" => SKColors.Yellow, "Black" => SKColors.Black, _ => SKColors.White }, IsAntialias = true })
        using (var subtitlePaint = new SKPaint { Color = textColor switch { "Yellow" => SKColors.Yellow, "Black" => SKColors.Black, _ => SKColors.White }, IsAntialias = true })
        using (var accentPaint = new SKPaint { Color = SKColors.White, IsAntialias = true })
        {
            var skTextAlignment = textAlignment switch
            {
                "Left" => SKTextAlign.Left,
                "Right" => SKTextAlign.Right,
                _ => SKTextAlign.Center
            };
            var textX = textAlignment switch
            {
                "Left" => 120f,
                "Right" => width - 120f,
                _ => width / 2f
            };
            var mediaFolder = Path.GetFullPath(Path.Combine(FileSystem.AppDataDirectory, "CreatorNexusMedia")) + Path.DirectorySeparatorChar;
            var safeImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : Path.GetFullPath(imagePath);
            if (safeImagePath is not null && safeImagePath.StartsWith(mediaFolder, StringComparison.OrdinalIgnoreCase) && File.Exists(safeImagePath))
            {
                using var sourceBitmap = SKBitmap.Decode(safeImagePath);
                if (sourceBitmap is not null && sourceBitmap.Width > 0 && sourceBitmap.Height > 0)
                {
                    var sourceRatio = (float)sourceBitmap.Width / sourceBitmap.Height;
                    var targetRatio = (float)width / height;
                    if (imageFit == "Fit")
                    {
                        canvas.Clear(background);
                        var drawWidth = sourceRatio > targetRatio ? width : height * sourceRatio;
                        var drawHeight = sourceRatio > targetRatio ? width / sourceRatio : height;
                        var left = (width - drawWidth) / 2f;
                        var top = (height - drawHeight) / 2f;
                        canvas.DrawBitmap(sourceBitmap, new SKRect(left, top, left + drawWidth, top + drawHeight));
                    }
                    else
                    {
                        SKRect sourceRect = sourceRatio > targetRatio
                            ? new SKRect((sourceBitmap.Width - sourceBitmap.Height * targetRatio) / 2f, 0, (sourceBitmap.Width + sourceBitmap.Height * targetRatio) / 2f, sourceBitmap.Height)
                            : new SKRect(0, (sourceBitmap.Height - sourceBitmap.Width / targetRatio) / 2f, sourceBitmap.Width, (sourceBitmap.Height + sourceBitmap.Width / targetRatio) / 2f);
                        canvas.DrawBitmap(sourceBitmap, sourceRect, new SKRect(0, 0, width, height));
                    }
                }
                else canvas.Clear(background);
            }
            else canvas.Clear(background);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(36, 36, width - 36, height - 36), 34, 34), panelPaint);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(70, height - 96, width - 70, height - 82), 7, 7), accentPaint);
            var titleLines = WrapThumbnailText(headline, titleFont, width - 150).Take(3).ToArray();
            var subtitleLines = WrapThumbnailText(subtitle, subtitleFont, width - 180).Take(2).ToArray();
            const float titleLineHeight = 88f;
            const float subtitleLineHeight = 44f;
            var totalHeight = titleLines.Length * titleLineHeight + (subtitleLines.Length > 0 ? 38 + subtitleLines.Length * subtitleLineHeight : 0);
            var y = textPlacement switch
            {
                "Top" => 110f + titleLineHeight * 0.78f,
                "Bottom" => height - totalHeight - 100f + titleLineHeight * 0.78f,
                _ => layout switch
                {
                    "Top banner" => 110f + titleLineHeight * 0.78f,
                    "Bottom banner" => height - totalHeight - 100f + titleLineHeight * 0.78f,
                    _ => (height - totalHeight) / 2f + titleLineHeight * 0.78f
                }
            };
            if (panelStyle != "None")
            {
                var panelInset = panelPadding switch { "Compact" => 16f, "Spacious" => 48f, _ => 30f };
                var panelTop = y - titleLineHeight * 0.78f - panelInset;
                var panelBottom = panelTop + totalHeight + panelInset * 1.5f;
                var panelLeft = textAlignment == "Left" ? 78f : textAlignment == "Right" ? width - 570f : 78f;
                var panelRight = textAlignment == "Left" ? 690f : textAlignment == "Right" ? width - 78f : width - 78f;
                var panelCornerRadius = panelCorners switch { "Square" => 0f, "Soft" => 12f, _ => 28f };
                var textPanelRect = new SKRoundRect(new SKRect(panelLeft, panelTop, panelRight, panelBottom), panelCornerRadius, panelCornerRadius);
                canvas.DrawRoundRect(textPanelRect, textPanelPaint);
                if (panelBorder != "None") canvas.DrawRoundRect(textPanelRect, textPanelBorderPaint);
            }
            foreach (var line in titleLines)
            {
                if (outlineColor != "None") canvas.DrawText(line, textX, y, skTextAlignment, titleFont, titleOutlinePaint);
                canvas.DrawText(line, textX, y, skTextAlignment, titleFont, titlePaint);
                y += titleLineHeight;
            }
            if (subtitleLines.Length > 0)
            {
                y += 12;
                foreach (var line in subtitleLines)
                {
                    if (outlineColor != "None") canvas.DrawText(line, textX, y, skTextAlignment, subtitleFont, subtitleOutlinePaint);
                    canvas.DrawText(line, textX, y, skTextAlignment, subtitleFont, subtitlePaint);
                    y += subtitleLineHeight;
                }
            }
            canvas.Flush();
        }
        var folder = Path.Combine(FileSystem.AppDataDirectory, "CreatorNexusExports");
        Directory.CreateDirectory(folder);
        var safeTitle = string.Concat(projectTitle.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "thumbnail";
        var extension = jpeg ? "jpg" : "png";
        var path = Path.Combine(folder, $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, jpeg ? 92 : 100);
        await using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private static IEnumerable<string> WrapThumbnailText(string text, SKFont font, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var line = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (line.Length > 0 && font.MeasureText(candidate) > maxWidth)
            {
                yield return line;
                line = word;
            }
            else line = candidate;
        }
        if (!string.IsNullOrWhiteSpace(line)) yield return line;
    }

    private async void OnEditScriptClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;

        var script = await EditLongTextAsync(
            _english ? "Script studio" : "Scriptstudio",
            project.Script,
            _english ? "Write your hook, scenes, dialogue and call to action…" : "Schrijf je opening, scènes, tekst en oproep tot actie…",
            $"script-draft-{project.Id}");
        if (script is null) return;
        await _store.UpdateAsync(project with { Script = script.Trim(), UpdatedAt = DateTimeOffset.UtcNow });
        await LoadProjectsAsync();
    }

    private async void OnAddMediaFileClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = _english ? "Choose a media file" : "Kies een mediabestand"
            });
            if (file is null) return;

            var safeName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "media-file";
            var folder = Path.Combine(FileSystem.AppDataDirectory, "CreatorNexusMedia");
            Directory.CreateDirectory(folder);
            var storedName = $"{Guid.NewGuid():N}_{safeName}";
            var destination = Path.Combine(folder, storedName);
            await using (var input = await file.OpenReadAsync())
            await using (var output = File.Create(destination))
                await input.CopyToAsync(output);

            var reference = $"{safeName} | {destination}";
            var updatedLibrary = string.IsNullOrWhiteSpace(project.MediaLibrary)
                ? reference
                : project.MediaLibrary + Environment.NewLine + reference;
            await _store.UpdateAsync(project with
            {
                MediaLibrary = updatedLibrary,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await LoadProjectsAsync();
            await DisplayAlertAsync(
                _english ? "Media added" : "Media toegevoegd",
                _english ? $"Copied “{safeName}” into CreatorNexus app storage." : $"“{safeName}” is gekopieerd naar de opslag van CreatorNexus.",
                _english ? "OK" : "Oké");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                _english ? "Could not add media" : "Media toevoegen mislukt",
                _english ? $"The file could not be copied: {ex.Message}" : $"Het bestand kon niet worden gekopieerd: {ex.Message}",
                _english ? "OK" : "Oké");
        }
    }

    private async void OnEditMediaLibraryClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;

        var entries = (project.MediaLibrary ?? string.Empty)
            .Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var actions = _english
            ? new[] { "Open/share selected file", "Remove selected entry", "Rename display name", "Edit references", "File information", "Cancel" }
            : new[] { "Bestand openen/delen", "Geselecteerde verwijzing verwijderen", "Weergavenaam wijzigen", "Verwijzingen bewerken", "Bestandsinformatie", "Annuleren" };
        if (entries.Count == 0)
        {
            var emptyAction = await DisplayActionSheetAsync(
                _english ? "Project media library is empty" : "De mediabibliotheek is leeg",
                _english ? "Cancel" : "Annuleren", null,
                _english ? "Add a text reference" : "Tekstverwijzing toevoegen");
            if (emptyAction == (_english ? "Add a text reference" : "Tekstverwijzing toevoegen"))
                await EditMediaReferencesAsync(project);
            return;
        }

        var availableCount = 0;
        var missingCount = 0;
        long totalBytes = 0;
        foreach (var entry in entries)
        {
            var separator = entry.IndexOf(" | ", StringComparison.Ordinal);
            var path = separator >= 0 ? entry[(separator + 3)..].Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                availableCount++;
                try { totalBytes += new FileInfo(path).Length; }
                catch { /* Keep the overview usable if file metadata cannot be read. */ }
            }
            else
            {
                missingCount++;
            }
        }

        var totalSize = totalBytes < 1024L * 1024L
            ? $"{Math.Max(1, (totalBytes + 1023) / 1024)} KB"
            : $"{totalBytes / (1024d * 1024d):0.0} MB";
        var libraryTitle = _english
            ? $"Media library • {entries.Count} items • {availableCount} local • {missingCount} missing • {totalSize}"
            : $"Mediabibliotheek • {entries.Count} items • {availableCount} lokaal • {missingCount} ontbreekt • {totalSize}";

        var selectedEntry = await DisplayActionSheetAsync(
            libraryTitle,
            _english ? "Cancel" : "Annuleren", null,
            entries.Select((line, index) => $"{index + 1}. {GetMediaSelectionLabel(line)}").ToArray());
        var selectedIndex = entries
            .Select((line, index) => new { line, index, label = $"{index + 1}. {GetMediaSelectionLabel(line)}" })
            .Where(item => item.label == selectedEntry)
            .Select(item => item.index)
            .DefaultIfEmpty(-1)
            .First();
        if (selectedIndex < 0) return;

        var selectedLine = entries[selectedIndex];
        var action = await DisplayActionSheetAsync(
            GetMediaDisplayName(selectedLine),
            _english ? "Cancel" : "Annuleren", null, actions.Take(5).ToArray());
        if (action == actions[0])
        {
            var separator = selectedLine.IndexOf(" | ", StringComparison.Ordinal);
            var path = separator >= 0 ? selectedLine[(separator + 3)..].Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    var extension = Path.GetExtension(path).ToLowerInvariant();
                    var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif" };
                    if (imageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        await ShowMediaImagePreviewAsync(path, GetMediaDisplayName(selectedLine));
                    }
                    else
                    {
                        await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = GetMediaDisplayName(selectedLine),
                            File = new ShareFile(path)
                        });
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync(
                        _english ? "Cannot open file" : "Bestand openen mislukt",
                        ex.Message, _english ? "OK" : "Oké");
                }
            }
            else
            {
                await DisplayAlertAsync(
                    _english ? "No local file" : "Geen lokaal bestand",
                    _english ? "This entry is only a reference or the file is missing." : "Dit item is alleen een verwijzing of het bestand ontbreekt.",
                    _english ? "OK" : "Oké");
            }
        }
        else if (action == actions[1])
        {
            var confirmed = await DisplayAlertAsync(
                _english ? "Remove media entry?" : "Media-item verwijderen?",
                _english ? $"Remove “{GetMediaDisplayName(selectedLine)}” from this project? A copied app file will also be deleted." : $"“{GetMediaDisplayName(selectedLine)}” uit dit project verwijderen? Een gekopieerd appbestand wordt ook verwijderd.",
                _english ? "Remove" : "Verwijderen",
                _english ? "Cancel" : "Annuleren");
            if (!confirmed) return;

            var separator = selectedLine.IndexOf(" | ", StringComparison.Ordinal);
            var path = separator >= 0 ? selectedLine[(separator + 3)..].Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    var mediaRoot = Path.GetFullPath(Path.Combine(FileSystem.AppDataDirectory, "CreatorNexusMedia")) + Path.DirectorySeparatorChar;
                    var fullPath = Path.GetFullPath(path);
                    if (fullPath.StartsWith(mediaRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                        File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync(
                        _english ? "File could not be deleted" : "Bestand kon niet worden verwijderd",
                        ex.Message, _english ? "OK" : "Oké");
                    return;
                }
            }

            entries.RemoveAt(selectedIndex);
            await _store.UpdateAsync(project with
            {
                MediaLibrary = string.Join(Environment.NewLine, entries),
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await LoadProjectsAsync();
        }
        else if (action == actions[2])
        {
            var currentName = GetMediaDisplayName(selectedLine);
            var newName = await DisplayPromptAsync(
                _english ? "Rename media display name" : "Weergavenaam van media wijzigen",
                _english ? "This changes the label shown in CreatorNexus, not the original file." : "Dit wijzigt alleen de naam die CreatorNexus toont, niet het originele bestand.",
                _english ? "Save" : "Opslaan",
                _english ? "Cancel" : "Annuleren",
                initialValue: currentName,
                maxLength: 180);
            if (string.IsNullOrWhiteSpace(newName)) return;

            var separator = selectedLine.IndexOf(" | ", StringComparison.Ordinal);
            entries[selectedIndex] = separator >= 0
                ? newName.Trim() + " | " + selectedLine[(separator + 3)..].Trim()
                : newName.Trim();
            await _store.UpdateAsync(project with
            {
                MediaLibrary = string.Join(Environment.NewLine, entries),
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await LoadProjectsAsync();
        }
        else if (action == actions[3])
        {
            await EditMediaReferencesAsync(project);
        }
        else if (action == actions[4])
        {
            var separator = selectedLine.IndexOf(" | ", StringComparison.Ordinal);
            var path = separator >= 0 ? selectedLine[(separator + 3)..].Trim() : string.Empty;
            string details;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var info = new FileInfo(path);
                var size = info.Length < 1024 * 1024
                    ? $"{Math.Max(1, (info.Length + 1023) / 1024)} KB"
                    : $"{info.Length / (1024d * 1024d):0.0} MB";
                details = _english
                    ? $"Name: {GetMediaDisplayName(selectedLine)}\nType: {info.Extension.ToUpperInvariant()}\nSize: {size}\nStatus: Available locally"
                    : $"Naam: {GetMediaDisplayName(selectedLine)}\nType: {info.Extension.ToUpperInvariant()}\nGrootte: {size}\nStatus: Lokaal beschikbaar";
            }
            else
            {
                details = _english
                    ? $"Name: {GetMediaDisplayName(selectedLine)}\nStatus: Reference only or local file missing.\nLocation: {(string.IsNullOrWhiteSpace(path) ? "No local path stored" : path)}"
                    : $"Naam: {GetMediaDisplayName(selectedLine)}\nStatus: Alleen verwijzing of lokaal bestand ontbreekt.\nLocatie: {(string.IsNullOrWhiteSpace(path) ? "Geen lokaal pad opgeslagen" : path)}";
            }

            await DisplayAlertAsync(
                _english ? "Media file information" : "Bestandsinformatie",
                details,
                _english ? "Close" : "Sluiten");
        }
    }

    private async Task ShowMediaImagePreviewAsync(string path, string displayName)
    {
        var image = new Image
        {
            Source = ImageSource.FromFile(path),
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        var closeButton = new Button
        {
            Text = _english ? "Close preview" : "Voorbeeld sluiten",
            BackgroundColor = Color.FromArgb("#30264A"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var previewPage = new ContentPage
        {
            Title = displayName,
            BackgroundColor = Color.FromArgb("#11111B"),
            Padding = new Thickness(16),
            Content = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
                RowSpacing = 12,
                Children = { image, closeButton }
            }
        };
        Grid.SetRow(image, 0);
        Grid.SetRow(closeButton, 1);
        closeButton.Clicked += async (_, _) => await Navigation.PopModalAsync();
        await Navigation.PushModalAsync(previewPage);
    }

    private string GetMediaSelectionLabel(string entry)
    {
        var displayName = GetMediaDisplayName(entry);
        var separator = entry.IndexOf(" | ", StringComparison.Ordinal);
        if (separator < 0)
            return displayName;

        var path = entry[(separator + 3)..].Trim();
        if (string.IsNullOrWhiteSpace(path) || File.Exists(path))
            return displayName;

        return $"{displayName} ({(_english ? "file missing" : "bestand ontbreekt")})";
    }

    private static string GetMediaDisplayName(string entry)
    {
        var separator = entry.IndexOf(" | ", StringComparison.Ordinal);
        return separator >= 0 ? entry[..separator].Trim() : entry.Trim();
    }

    private async Task EditMediaReferencesAsync(CreatorProject project)
    {
        var media = await EditLongTextAsync(
            _english ? "Project media references" : "Media-verwijzingen van project",
            project.MediaLibrary,
            _english ? "Add one filename, file location or media URL per line. Imported files are stored in app-private storage." : "Voeg per regel een bestandsnaam, locatie of media-URL toe. Geïmporteerde bestanden staan in de privéopslag van de app.");
        if (media is null) return;
        await _store.UpdateAsync(project with { MediaLibrary = media.Trim(), UpdatedAt = DateTimeOffset.UtcNow });
        await LoadProjectsAsync();
    }

    private async Task<string?> EditLongTextAsync(string title, string initialValue, string placeholder, string? draftKey = null)
    {
        if (!string.IsNullOrWhiteSpace(draftKey) && Preferences.ContainsKey(draftKey))
        {
            var recoveredDraft = Preferences.Get(draftKey, string.Empty);
            if (!string.IsNullOrEmpty(recoveredDraft) && !string.Equals(recoveredDraft, initialValue, StringComparison.Ordinal))
            {
                var recover = await DisplayAlertAsync(
                    title,
                    _english ? "A recovered draft is available. Would you like to restore it?" : "Er is een hersteld concept beschikbaar. Wil je dit terugzetten?",
                    _english ? "Restore" : "Herstellen",
                    _english ? "Discard draft" : "Concept weggooien");
                if (recover)
                    initialValue = recoveredDraft;
                else
                    Preferences.Remove(draftKey);
            }
            else
            {
                Preferences.Remove(draftKey);
            }
        }

        var editor = new Editor
        {
            Text = initialValue,
            Placeholder = placeholder,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 240,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            BackgroundColor = Color.FromArgb("#262638"),
            VerticalOptions = LayoutOptions.Fill
        };
        var countLabel = new Label
        {
            TextColor = Color.FromArgb("#A5B4FC"),
            FontSize = 12
        };
        SemanticProperties.SetDescription(
            countLabel,
            _english ? "Word and character count" : "Aantal woorden en tekens");
        void UpdateTextCount()
        {
            var value = editor.Text ?? string.Empty;
            var words = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            countLabel.Text = _english
                ? $"{words} words • {value.Length} characters"
                : $"{words} woorden • {value.Length} tekens";
        }
        editor.TextChanged += (_, _) =>
        {
            UpdateTextCount();
            if (!string.IsNullOrWhiteSpace(draftKey))
                Preferences.Set(draftKey, editor.Text ?? string.Empty);
        };
        UpdateTextCount();

        var completion = new TaskCompletionSource<string?>();
        var saveButton = new Button
        {
            Text = _english ? "Save" : "Opslaan",
            BackgroundColor = Color.FromArgb("#7C3AED"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var cancelButton = new Button
        {
            Text = _english ? "Cancel" : "Annuleren",
            BackgroundColor = Color.FromArgb("#262638"),
            TextColor = Colors.White,
            CornerRadius = 12
        };
        var page = new ContentPage
        {
            Title = title,
            BackgroundColor = Color.FromArgb("#11111B"),
            Padding = new Thickness(18),
            Content = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                RowSpacing = 14,
                Children =
                {
                    new Label { Text = title, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    editor,
                    countLabel,
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                        ColumnSpacing = 10,
                        Children = { cancelButton, saveButton }
                    }
                }
            }
        };
        var root = (Grid)page.Content;
        Grid.SetRow(editor, 1);
        Grid.SetRow(countLabel, 2);
        Grid.SetRow((View)root.Children[3], 3);
        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(saveButton, 1);

        saveButton.Clicked += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(draftKey))
                Preferences.Remove(draftKey);
            await Navigation.PopModalAsync();
            completion.TrySetResult(editor.Text ?? string.Empty);
        };
        cancelButton.Clicked += async (_, _) =>
        {
            await Navigation.PopModalAsync();
            completion.TrySetResult(null);
        };
        await Navigation.PushModalAsync(page);
        return await completion.Task;
    }

    private async void OnCreateFromTemplateClicked(object sender, EventArgs e)
    {
        var choices = _english
            ? new[] { "YouTube video", "Short-form video", "Tutorial", "Livestream", "Cancel" }
            : new[] { "YouTube-video", "Korte video", "Tutorial", "Livestream", "Annuleren" };
        var selected = await DisplayActionSheetAsync(
            _english ? "Choose a project template" : "Kies een projectsjabloon",
            _english ? "Cancel" : "Annuleren",
            null,
            choices.Take(choices.Length - 1).ToArray());
        if (string.IsNullOrWhiteSpace(selected) || selected == (_english ? "Cancel" : "Annuleren"))
            return;

        var isShort = selected == (_english ? "Short-form video" : "Korte video");
        var isTutorial = selected == "Tutorial";
        var isLive = selected == "Livestream";
        var project = new CreatorProject
        {
            Title = selected,
            Channel = isLive ? "Twitch / YouTube" : "YouTube",
            Tags = isShort ? "short, video" : isTutorial ? "tutorial, uitleg" : isLive ? "live, stream" : "video, creator",
            Notes = _english
                ? "Add the idea, audience and call to action here."
                : "Vul hier het idee, de doelgroep en de oproep tot actie in.",
            Checklist = _english
                ? "Define the idea; Prepare script or outline; Record or create; Edit and review; Prepare title and thumbnail; Publish and share"
                : "Idee bepalen; Script of opzet voorbereiden; Opnemen of maken; Monteren en controleren; Titel en thumbnail voorbereiden; Publiceren en delen"
        };
        await _store.AddAsync(project);
        _showArchived = false;
        _showFavoritesOnly = false;
        _showScheduleOnly = false;
        ProjectSearchBar.Text = string.Empty;
        await LoadProjectsAsync();
    }

    private async void OnEditChecklistClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var markNextTask = _english ? "Mark next open task complete" : "Volgende open taak afvinken";
        var reopenTask = _english ? "Reopen last completed task" : "Laatste afgeronde taak heropenen";
        var templateNames = _english
            ? new[] { "Custom / keep current", "YouTube video", "Short / Reel / TikTok", "Podcast episode", "Livestream", "Thumbnail design", markNextTask, reopenTask }
            : new[] { "Zelf aanpassen / huidige behouden", "YouTube-video", "Short / Reel / TikTok", "Podcastaflevering", "Livestream", "Thumbnail ontwerpen", markNextTask, reopenTask };
        var templateChoice = await DisplayActionSheetAsync(
            _english ? "Checklist template" : "Checklist-sjabloon",
            _english ? "Cancel" : "Annuleren", null, templateNames);
        if (templateChoice == (_english ? "Cancel" : "Annuleren") || templateChoice is null)
            return;

        if (templateChoice == markNextTask || templateChoice == reopenTask)
        {
            var tasks = project.Checklist
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var targetIndex = templateChoice == markNextTask
                ? tasks.FindIndex(task => !task.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
                : tasks.FindLastIndex(task => task.StartsWith("[x]", StringComparison.OrdinalIgnoreCase));

            if (targetIndex < 0)
            {
                await DisplayAlertAsync(
                    _english ? "Checklist" : "Checklist",
                    templateChoice == markNextTask
                        ? (_english ? "There are no open tasks to complete." : "Er zijn geen open taken om af te vinken.")
                        : (_english ? "There are no completed tasks to reopen." : "Er zijn geen afgeronde taken om te heropenen."),
                    "OK");
                return;
            }

            var taskText = tasks[targetIndex]
                .Replace("[x]", "", StringComparison.OrdinalIgnoreCase)
                .Replace("[ ]", "", StringComparison.Ordinal)
                .Trim();
            tasks[targetIndex] = templateChoice == markNextTask ? $"[x] {taskText}" : $"[ ] {taskText}";

            await _store.UpdateAsync(project with
            {
                Checklist = string.Join("; ", tasks),
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await LoadProjectsAsync();
            return;
        }

        var templateTasks = templateChoice switch
        {
            "YouTube video" or "YouTube-video" => _english
                ? new[] { "Define topic and goal", "Prepare script / outline", "Record video and audio", "Edit the video", "Check thumbnail and title", "Prepare description, tags and subtitles", "Review the final video", "Schedule or publish" }
                : new[] { "Onderwerp en doel bepalen", "Script of opzet voorbereiden", "Video en audio opnemen", "Video monteren", "Thumbnail en titel controleren", "Beschrijving, tags en ondertiteling voorbereiden", "Eindvideo controleren", "Inplannen of publiceren" },
            "Short / Reel / TikTok" => _english
                ? new[] { "Create a hook for the first seconds", "Set vertical 9:16 framing", "Record the clip", "Check captions", "Check audio and usage rights", "Prepare cover and caption", "Preview and publish" }
                : new[] { "Aandachtstrekker voor de eerste seconden bedenken", "Verticale beeldverhouding 9:16 instellen", "Clip opnemen", "Ondertiteling controleren", "Audio en gebruiksrechten controleren", "Cover en bijschrift voorbereiden", "Voorbeeld bekijken en publiceren" },
            "Podcast episode" or "Podcastaflevering" => _english
                ? new[] { "Confirm topic and guest", "Prepare questions / outline", "Test microphones", "Record the episode", "Edit audio", "Prepare intro, outro and show notes", "Listen to final export", "Schedule or publish" }
                : new[] { "Onderwerp en gast bevestigen", "Vragen of opzet voorbereiden", "Microfoons testen", "Aflevering opnemen", "Audio monteren", "Intro, outro en shownotes voorbereiden", "Definitieve export beluisteren", "Inplannen of publiceren" },
            "Livestream" => _english
                ? new[] { "Set stream title and category", "Test scenes and audio", "Check internet and recording", "Prepare alerts and chat", "Announce the stream", "Start the stream", "Save VOD / highlights", "Review after the stream" }
                : new[] { "Streamtitel en categorie instellen", "Scènes en audio testen", "Internetverbinding en opname controleren", "Alerts en chat voorbereiden", "Stream aankondigen", "Stream starten", "VOD of hoogtepunten opslaan", "Na afloop evalueren" },
            "Thumbnail design" or "Thumbnail ontwerpen" => _english
                ? new[] { "Choose the main image", "Set headline and visual hierarchy", "Check contrast and readability", "Check mobile-size preview", "Check crop / image framing", "Export PNG or JPG", "Review final file" }
                : new[] { "Hoofdafbeelding kiezen", "Koptekst en visuele hiërarchie instellen", "Contrast en leesbaarheid controleren", "Voorbeeld op mobiel formaat controleren", "Uitsnede en beeldkader controleren", "Exporteren als PNG of JPG", "Eindbestand controleren" },
            _ => Array.Empty<string>()
        };
        var templateText = string.Join("; ", templateTasks);
        var initialChecklist = string.IsNullOrWhiteSpace(templateText)
            ? project.Checklist
            : string.IsNullOrWhiteSpace(project.Checklist)
                ? templateText
                : $"{project.Checklist.Trim().TrimEnd(';')}; {templateText}";

        var checklist = await DisplayPromptAsync(
            _english ? "Production checklist" : "Productiechecklist",
            _english
                ? "Separate tasks with semicolons. Prefix a completed task with [x] and an open task with [ ]. Example: [x] Script ready; [ ] Record video."
                : "Scheid taken met puntkomma’s. Zet [x] vóór een afgeronde taak en [ ] vóór een open taak. Voorbeeld: [x] Script klaar; [ ] Video opnemen.",
            _english ? "Save" : "Opslaan",
            _english ? "Cancel" : "Annuleren",
            maxLength: 3000,
            initialValue: initialChecklist);
        if (checklist is null)
            return;

        await _store.UpdateAsync(project with
        {
            Checklist = checklist.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await LoadProjectsAsync();
    }

    private async void OnAddProjectClicked(object sender, EventArgs e)
    {
        var title = NewProjectEntry.Text;
        var error = ProjectRules.ValidateTitle(title);

        if (!string.IsNullOrEmpty(error))
        {
            await DisplayAlertAsync(
                _english ? "Check the title" : "Controleer de titel",
                _english ? "Enter a title of up to 120 characters." : "Vul een titel in van maximaal 120 tekens.",
                _english ? "OK" : "Oké");
            return;
        }

        await _store.AddAsync(new CreatorProject
        {
            Title = title!.Trim(),
            Channel = _english ? "My channel" : "Mijn kanaal"
        });

        NewProjectEntry.Text = string.Empty;
        ProjectSearchBar.Text = string.Empty;
        _showArchived = false;
        await LoadProjectsAsync();
    }

    private async void OnAdvanceStatusClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var next = project.Status == ProjectStatus.Published
            ? ProjectStatus.Idea
            : (ProjectStatus)((int)project.Status + 1);

        await _store.UpdateAsync(project.WithStatus(next));
        await LoadProjectsAsync();
    }

    private async void OnEditProjectClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var title = await DisplayPromptAsync(
            _english ? "Edit project title" : "Projecttitel wijzigen",
            _english ? "Choose a title (max. 120 characters)." : "Kies een titel (maximaal 120 tekens).",
            _english ? "Next" : "Volgende",
            _english ? "Cancel" : "Annuleren",
            maxLength: 120,
            initialValue: project.Title);

        if (title is null)
            return;

        var titleError = ProjectRules.ValidateTitle(title);
        if (!string.IsNullOrEmpty(titleError))
        {
            await DisplayAlertAsync(
                _english ? "Invalid title" : "Ongeldige titel",
                _english ? "Enter a non-empty title of up to 120 characters." : "Vul een titel in van maximaal 120 tekens.",
                _english ? "OK" : "Oké");
            return;
        }

        var channel = await DisplayPromptAsync(
            _english ? "Edit channel" : "Kanaal wijzigen",
            _english ? "Enter the platform or channel name." : "Vul de platform- of kanaalnaam in.",
            _english ? "Save" : "Opslaan",
            _english ? "Cancel" : "Annuleren",
            maxLength: 120,
            initialValue: project.Channel);

        if (channel is null)
            return;

        if (string.IsNullOrWhiteSpace(channel) || channel.Trim().Length > 120)
        {
            await DisplayAlertAsync(
                _english ? "Invalid channel" : "Ongeldig kanaal",
                _english ? "Enter a channel name of 1 to 120 characters." : "Vul een kanaalnaam in van 1 tot 120 tekens.",
                _english ? "OK" : "Oké");
            return;
        }

        await _store.UpdateAsync(project with
        {
            Title = title.Trim(),
            Channel = channel.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await LoadProjectsAsync();
    }

    private async void OnToggleFavoriteClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;
        await _store.UpdateAsync(project with { IsFavorite = !project.IsFavorite, UpdatedAt = DateTimeOffset.UtcNow });
        await LoadProjectsAsync();
    }

    private async void OnScheduleProjectClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;

        var current = project.ScheduledFor?.ToLocalTime().ToString("dd-MM-yyyy") ?? string.Empty;
        var dateText = await DisplayPromptAsync(
            _english ? "Schedule content" : "Content inplannen",
            _english ? "Enter a date as DD-MM-YYYY, or leave blank to clear it." : "Vul een datum in als DD-MM-JJJJ, of laat leeg om de planning te wissen.",
            _english ? "Save" : "Opslaan",
            _english ? "Cancel" : "Annuleren",
            placeholder: "DD-MM-YYYY",
            maxLength: 10,
            initialValue: current);
        if (dateText is null) return;

        DateTimeOffset? scheduled = null;
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            if (!DateTime.TryParseExact(dateText.Trim(), "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
            {
                await DisplayAlertAsync(
                    _english ? "Invalid date" : "Ongeldige datum",
                    _english ? "Use DD-MM-YYYY, for example 25-12-2026." : "Gebruik DD-MM-JJJJ, bijvoorbeeld 25-12-2026.",
                    _english ? "OK" : "Oké");
                return;
            }
            scheduled = new DateTimeOffset(date.Date, TimeZoneInfo.Local.GetUtcOffset(date.Date));
        }

        await _store.UpdateAsync(project with { ScheduledFor = scheduled, UpdatedAt = DateTimeOffset.UtcNow });
        await LoadProjectsAsync();
    }

    private async void OnEditTagsClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var tags = await DisplayPromptAsync(
            _english ? "Project tags" : "Projecttags",
            _english ? "Separate tags with commas, for example gaming, tutorial, short." : "Scheid tags met komma's, bijvoorbeeld gaming, uitleg, short.",
            _english ? "Save" : "Opslaan",
            _english ? "Cancel" : "Annuleren",
            placeholder: _english ? "tag1, tag2, tag3" : "tag1, tag2, tag3",
            maxLength: 300,
            initialValue: project.Tags);

        if (tags is null)
            return;

        var normalizedTags = string.Join(", ",
            tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(tag => tag.Length <= 40)
                .Distinct(StringComparer.OrdinalIgnoreCase));

        await _store.UpdateAsync(project with
        {
            Tags = normalizedTags,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await LoadProjectsAsync();
    }

    private async void OnEditNotesClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var notes = await DisplayPromptAsync(
            _english ? "Project notes" : "Projectnotities",
            _english ? "Add an idea, next step or reminder." : "Voeg een idee, volgende stap of herinnering toe.",
            _english ? "Save" : "Opslaan",
            _english ? "Cancel" : "Annuleren",
            _english ? "Write notes here…" : "Schrijf hier je notities…",
            maxLength: 4000,
            initialValue: project.Notes);

        if (notes is null)
            return;

        await _store.UpdateAsync(project with
        {
            Notes = notes.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await LoadProjectsAsync();
    }

    private async void OnToggleArchiveClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        await _store.UpdateAsync(project.WithArchived(!project.IsArchived));
        await LoadProjectsAsync();
    }

    private async void OnDuplicateProjectClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var suffix = _english ? " (copy)" : " (kopie)";
        var title = project.Title;
        if (title.Length + suffix.Length > 120)
            title = title[..(120 - suffix.Length)];
        var now = DateTimeOffset.UtcNow;
        var duplicate = project with
        {
            Id = Guid.NewGuid(),
            Title = title + suffix,
            Status = ProjectStatus.Idea,
            IsArchived = false,
            IsFavorite = false,
            ScheduledFor = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _store.AddAsync(duplicate);
        _showArchived = false;
        _showFavoritesOnly = false;
        _showScheduleOnly = false;
        ProjectSearchBar.Text = string.Empty;
        await LoadProjectsAsync();
        await DisplayAlertAsync(
            _english ? "Project duplicated" : "Project gekopieerd",
            _english
                ? $"A new draft “{duplicate.Title}” was created. Its script, checklist, notes and design settings were copied."
                : $"Een nieuw concept “{duplicate.Title}” is gemaakt. Script, checklist, notities en ontwerpinstellingen zijn meegekopieerd.",
            _english ? "OK" : "Oké");
    }

    private async void OnDeleteProjectClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
            return;

        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null)
            return;

        var confirmed = await DisplayAlertAsync(
            _english ? "Delete project?" : "Project verwijderen?",
            _english
                ? $"“{project.Title}” and its notes will be permanently deleted."
                : $"“{project.Title}” en de bijbehorende notities worden definitief verwijderd.",
            _english ? "Delete" : "Verwijderen",
            _english ? "Cancel" : "Annuleren");

        if (!confirmed)
            return;

        await _store.DeleteAsync(id);
        await LoadProjectsAsync();
    }

    private async void OnToggleFavoritesFilterClicked(object sender, EventArgs e)
    {
        _showFavoritesOnly = !_showFavoritesOnly;
        FavoriteFilterButton.BackgroundColor = _showFavoritesOnly ? Color.FromArgb("#59451D") : Color.FromArgb("#262638");
        await LoadProjectsAsync();
    }

    private async void OnToggleScheduleViewClicked(object sender, EventArgs e)
    {
        _showScheduleOnly = !_showScheduleOnly;
        ScheduleViewButton.BackgroundColor = _showScheduleOnly ? Color.FromArgb("#30264A") : Color.FromArgb("#262638");
        ProjectsHeading.Text = _showScheduleOnly
            ? (_english ? "Content calendar" : "Contentkalender")
            : (_english ? "Your projects" : "Jouw projecten");
        await LoadProjectsAsync();
    }

    private async void OnToggleArchivedViewClicked(object sender, EventArgs e)
    {
        _showArchived = !_showArchived;
        ArchiveViewButton.Text = _showArchived
            ? (_english ? "Active projects" : "Actieve projecten")
            : (_english ? "Archive" : "Archief");
        await LoadProjectsAsync();
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // Ignore stale reads if a newer keystroke finishes loading first.
        var requestVersion = ++_searchRequestVersion;
        var searchText = e.NewTextValue;

        try
        {
            // Wait briefly so rapid typing triggers one disk read instead of one
            // read per character. A newer keystroke cancels this pending search.
            await Task.Delay(180);
            if (requestVersion != _searchRequestVersion)
                return;

            var projects = await _store.LoadAsync();
            if (requestVersion != _searchRequestVersion)
                return;

            _searchErrorShown = false;
            RenderProjects(GetFilteredProjects(projects, searchText));
        }
        catch (Exception ex)
        {
            if (requestVersion != _searchRequestVersion)
                return;

            // TextChanged fires for each keystroke. Show one recovery alert per
            // continuous failure, not a cascade of identical dialogs.
            if (_searchErrorShown)
                return;

            _searchErrorShown = true;
            await DisplayAlertAsync(
                _english ? "Search failed" : "Zoeken mislukt",
                _english
                    ? "CreatorNexus could not read the saved projects. Your original data has been preserved. Details: " + ex.Message
                    : "CreatorNexus kon de opgeslagen projecten niet lezen. Je oorspronkelijke gegevens zijn bewaard. Details: " + ex.Message,
                "OK");
        }
    }

    private async void OnExportBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var backupPath = Path.Combine(FileSystem.CacheDirectory,
                $"CreatorNexus-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            await _store.ExportBackupAsync(backupPath);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = _english ? "Save or share CreatorNexus backup" : "CreatorNexus-back-up opslaan of delen",
                File = new ShareFile(backupPath, "application/json")
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                _english ? "Backup failed" : "Back-up mislukt",
                _english ? $"The backup could not be prepared: {ex.Message}" : $"De back-up kon niet worden klaargezet: {ex.Message}",
                _english ? "OK" : "Oké");
        }
    }

    private async void OnRestoreBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = _english ? "Choose a CreatorNexus JSON backup" : "Kies een CreatorNexus JSON-back-up",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/json", "text/json" },
                    [DevicePlatform.WinUI] = new[] { ".json" }
                })
            });
            if (picked is null)
                return;

            var confirmed = await DisplayAlertAsync(
                _english ? "Replace current projects?" : "Huidige projecten vervangen?",
                _english
                    ? "Restoring replaces the current project list with the selected backup. Continue? The previous saved file is retained as a local backup."
                    : "Terugzetten vervangt de huidige projectenlijst door de gekozen back-up. Doorgaan? Het vorige opgeslagen bestand blijft als lokale back-up bewaard.",
                _english ? "Restore" : "Terugzetten",
                _english ? "Cancel" : "Annuleren");
            if (!confirmed)
                return;

            var temporaryPath = Path.Combine(FileSystem.CacheDirectory, $"CreatorNexus-restore-{Guid.NewGuid():N}.json");
            try
            {
                await using (var source = await picked.OpenReadAsync())
                await using (var destination = File.Create(temporaryPath))
                    await source.CopyToAsync(destination);

                await _store.RestoreBackupAsync(temporaryPath);
                _showArchived = false;
                _showFavoritesOnly = false;
                _showScheduleOnly = false;
                ProjectSearchBar.Text = string.Empty;
                await LoadProjectsAsync();
                await DisplayAlertAsync(
                    _english ? "Backup restored" : "Back-up teruggezet",
                    _english ? "The projects from the backup have been restored." : "De projecten uit de back-up zijn teruggezet.",
                    _english ? "OK" : "Oké");
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                _english ? "Restore failed" : "Terugzetten mislukt",
                _english ? $"The backup could not be restored. Details: {ex.Message}" : $"De back-up kon niet worden teruggezet. Details: {ex.Message}",
                _english ? "OK" : "Oké");
        }
    }

    private async void OnRecoverLocalBackupClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            _english ? "Recover local backup?" : "Lokale back-up herstellen?",
            _english
                ? "This restores the last local .bak snapshot and replaces the current project file. The recovery backup itself is kept. Continue?"
                : "Dit zet de laatste lokale .bak-kopie terug en vervangt het huidige projectbestand. De herstelkopie zelf blijft behouden. Doorgaan?",
            _english ? "Recover" : "Herstellen",
            _english ? "Cancel" : "Annuleren");
        if (!confirmed)
            return;

        try
        {
            await _store.RecoverFromLocalBackupAsync();
            _showArchived = false;
            _showFavoritesOnly = false;
            _showScheduleOnly = false;
            ProjectSearchBar.Text = string.Empty;
            await LoadProjectsAsync();
            await DisplayAlertAsync(
                _english ? "Recovery complete" : "Herstel voltooid",
                _english
                    ? "The validated local backup has been restored. The .bak recovery file was preserved."
                    : "De gecontroleerde lokale back-up is teruggezet. Het .bak-herstelbestand is behouden.",
                _english ? "OK" : "Oké");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                _english ? "Recovery failed" : "Herstellen mislukt",
                _english
                    ? $"The local backup could not be restored. The saved project file was not changed by the recovery step. Details: {ex.Message}"
                    : $"De lokale back-up kon niet worden hersteld. Het opgeslagen projectbestand is door deze herstelactie niet gewijzigd. Details: {ex.Message}",
                _english ? "OK" : "Oké");
        }
    }

    private void OnLanguageClicked(object sender, EventArgs e)
    {
        _english = !_english;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        LanguageButton.Text = _english ? "NL" : "EN";
        SubtitleLabel.Text = _english ? "Your creative command center" : "Jouw creatieve commandocentrum";
        WelcomeLabel.Text = _english ? "Turn ideas into real projects." : "Maak van ideeën echte projecten.";
        IntroLabel.Text = _english
            ? "Capture ideas, organize content and keep your next step in sight."
            : "Bewaar ideeën, organiseer content en houd je volgende stap in zicht.";
        NewProjectEntry.Placeholder = _english ? "Name your new project…" : "Naam van je nieuwe project…";
        ExportBackupButton.Text = _english ? "Create backup" : "Back-up maken";
        RestoreBackupButton.Text = _english ? "Restore backup" : "Back-up terugzetten";
        RecoverLocalBackupButton.Text = _english ? "Recover local backup" : "Herstel lokale back-up";
        AddButton.Text = _english ? "+ New" : "+ Nieuw";
        TemplateButton.Text = _english ? "Template" : "Sjabloon";
        ProjectsHeading.Text = _showScheduleOnly
            ? (_english ? "Content calendar" : "Contentkalender")
            : (_english ? "Your projects" : "Jouw projecten");
        ProjectSearchBar.Placeholder = _english ? "Search projects…" : "Zoek projecten…";
        EmptyTitleLabel.Text = _english ? "No projects here yet" : "Hier staan nog geen projecten";
        EmptySubtitleLabel.Text = _showArchived
            ? (_english ? "Archived projects will appear here." : "Gearchiveerde projecten verschijnen hier.")
            : (_english ? "Create your first creator project above." : "Maak hierboven je eerste creatorproject.");
        FooterLabel.Text = _english
            ? "Local storage • Offline ready • Free"
            : "Lokale opslag • Offline beschikbaar • Gratis";
        ArchiveViewButton.Text = _showArchived
            ? (_english ? "Active projects" : "Actieve projecten")
            : (_english ? "Archive" : "Archief");

        _ = LoadProjectsAsync();
    }

    private void ApplyResponsiveLayout()
    {
        Padding = Width >= 1000
            ? new Thickness(44, 28)
            : Width >= 600
                ? new Thickness(28, 22)
                : new Thickness(18);
    }
}
