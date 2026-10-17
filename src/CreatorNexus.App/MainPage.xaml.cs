using SkiaSharp;
using System.Collections.ObjectModel;
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
        var projects = await _store.LoadAsync();
        RenderProjects(GetFilteredProjects(projects, ProjectSearchBar.Text));
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
    }

    private async void OnEditThumbnailClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id);
        if (project is null) return;

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
        var subtitleSizePicker = new Picker
        {
            Title = _english ? "Subtitle size" : "Grootte ondertitel",
            ItemsSource = subtitleSizes,
            SelectedItem = subtitleSizes.Contains(project.ThumbnailSubtitleSize) ? project.ThumbnailSubtitleSize : "Medium",
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
                    new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Fill,
                        Spacing = 10,
                        Children = { previewHeadline, previewSubtitle }
                    }
                }
            }
        };
        void RefreshPreview()
        {
            previewHeadline.Text = string.IsNullOrWhiteSpace(titleEntry.Text) ? (_english ? "Your headline" : "Jouw koptekst") : titleEntry.Text;
            previewSubtitle.Text = subtitleEntry.Text ?? string.Empty;
            var textAlignment = alignmentPicker.SelectedItem?.ToString() ?? "Center";
            previewHeadline.HorizontalTextAlignment = textAlignment switch
            {
                "Left" => TextAlignment.Start,
                "Right" => TextAlignment.End,
                _ => TextAlignment.Center
            };
            previewSubtitle.HorizontalTextAlignment = previewHeadline.HorizontalTextAlignment;
            previewHeadline.FontSize = titleSizePicker.SelectedItem?.ToString() switch { "Small" => 18, "Large" => 30, _ => 24 };
            previewSubtitle.FontSize = subtitleSizePicker.SelectedItem?.ToString() switch { "Small" => 11, "Large" => 18, _ => 14 };
            var layout = layoutPicker.SelectedItem?.ToString() ?? "Center";
            var previewStack = (VerticalStackLayout)((Grid)preview.Content).Children[1];
            previewStack.VerticalOptions = layout switch
            {
                "Top banner" => LayoutOptions.Start,
                "Bottom banner" => LayoutOptions.End,
                _ => LayoutOptions.Center
            };
            previewStack.Padding = layout == "Center" ? new Thickness(0) : new Thickness(0, 8);
            var theme = picker.SelectedItem?.ToString() ?? "Purple";
            preview.BackgroundColor = Color.FromArgb(theme switch
            {
                "Blue" => "#155EAA",
                "Orange" => "#C2410C",
                "Green" => "#166534",
                _ => "#6D28D9"
            });
        }
        titleEntry.TextChanged += (_, _) => RefreshPreview();
        subtitleEntry.TextChanged += (_, _) => RefreshPreview();
        picker.SelectedIndexChanged += (_, _) => RefreshPreview();
        alignmentPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        titleSizePicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        subtitleSizePicker.SelectedIndexChanged += (_, _) => RefreshPreview();
        layoutPicker.SelectedIndexChanged += (_, _) => RefreshPreview();
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
                        new Label { Text = _english ? "Layout" : "Indeling", TextColor = Colors.White },
                        layoutPicker,
                        chooseImageButton,
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
                var exportPath = await ExportThumbnailPngAsync(project.Title, headline, subtitle, theme, selectedImagePath, alignmentPicker.SelectedItem?.ToString() ?? "Center", titleSizePicker.SelectedItem?.ToString() ?? "Medium", subtitleSizePicker.SelectedItem?.ToString() ?? "Medium", layoutPicker.SelectedItem?.ToString() ?? "Center");
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
                var exportPath = await ExportThumbnailPngAsync(project.Title, headline, subtitle, theme, selectedImagePath, alignmentPicker.SelectedItem?.ToString() ?? "Center", titleSizePicker.SelectedItem?.ToString() ?? "Medium", subtitleSizePicker.SelectedItem?.ToString() ?? "Medium", layoutPicker.SelectedItem?.ToString() ?? "Center", true);
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
            ThumbnailTextAlignment = alignmentPicker.SelectedItem?.ToString() ?? "Center",
            ThumbnailTitleSize = titleSizePicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailSubtitleSize = subtitleSizePicker.SelectedItem?.ToString() ?? "Medium",
            ThumbnailLayout = layoutPicker.SelectedItem?.ToString() ?? "Center",
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await LoadProjectsAsync();
    }

    private async Task<string> ExportThumbnailPngAsync(string projectTitle, string headline, string subtitle, string theme, string? imagePath, string textAlignment = "Center", string titleSize = "Medium", string subtitleSize = "Medium", string layout = "Center", bool jpeg = false)
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
        using (var canvas = new SKCanvas(bitmap))
        using (var panelPaint = new SKPaint { Color = new SKColor(12, 12, 24, 70), IsAntialias = true })
        using (var titlePaint = new SKPaint { Color = SKColors.White, TextSize = titleSize switch { "Small" => 60, "Large" => 92, _ => 76 }, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), TextAlign = SKTextAlign.Center })
        using (var subtitlePaint = new SKPaint { Color = new SKColor(245, 245, 255), TextSize = subtitleSize switch { "Small" => 26, "Large" => 42, _ => 34 }, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal), TextAlign = SKTextAlign.Center })
        using (var accentPaint = new SKPaint { Color = SKColors.White, IsAntialias = true })
        {
            var skTextAlignment = textAlignment switch
            {
                "Left" => SKTextAlign.Left,
                "Right" => SKTextAlign.Right,
                _ => SKTextAlign.Center
            };
            titlePaint.TextAlign = skTextAlignment;
            subtitlePaint.TextAlign = skTextAlignment;
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
                    SKRect sourceRect = sourceRatio > targetRatio
                        ? new SKRect((sourceBitmap.Width - sourceBitmap.Height * targetRatio) / 2f, 0, (sourceBitmap.Width + sourceBitmap.Height * targetRatio) / 2f, sourceBitmap.Height)
                        : new SKRect(0, (sourceBitmap.Height - sourceBitmap.Width / targetRatio) / 2f, sourceBitmap.Width, (sourceBitmap.Height + sourceBitmap.Width / targetRatio) / 2f);
                    canvas.DrawBitmap(sourceBitmap, sourceRect, new SKRect(0, 0, width, height));
                }
                else canvas.Clear(background);
            }
            else canvas.Clear(background);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(36, 36, width - 36, height - 36), 34, 34), panelPaint);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(70, height - 96, width - 70, height - 82), 7, 7), accentPaint);
            var titleLines = WrapThumbnailText(headline, titlePaint, width - 150).Take(3).ToArray();
            var subtitleLines = WrapThumbnailText(subtitle, subtitlePaint, width - 180).Take(2).ToArray();
            const float titleLineHeight = 88f;
            const float subtitleLineHeight = 44f;
            var totalHeight = titleLines.Length * titleLineHeight + (subtitleLines.Length > 0 ? 38 + subtitleLines.Length * subtitleLineHeight : 0);
            var y = layout switch
            {
                "Top banner" => 110f + titleLineHeight * 0.78f,
                "Bottom banner" => height - totalHeight - 100f + titleLineHeight * 0.78f,
                _ => (height - totalHeight) / 2f + titleLineHeight * 0.78f
            };
            foreach (var line in titleLines) { canvas.DrawText(line, textX, y, titlePaint); y += titleLineHeight; }
            if (subtitleLines.Length > 0)
            {
                y += 12;
                foreach (var line in subtitleLines) { canvas.DrawText(line, textX, y, subtitlePaint); y += subtitleLineHeight; }
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

    private static IEnumerable<string> WrapThumbnailText(string text, SKPaint paint, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var line = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (line.Length > 0 && paint.MeasureText(candidate) > maxWidth)
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
            _english ? "Write your hook, scenes, dialogue and call to action…" : "Schrijf je opening, scènes, tekst en oproep tot actie…");
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
            ? new[] { "Open/share selected file", "Remove selected entry", "Edit references", "Cancel" }
            : new[] { "Bestand openen/delen", "Geselecteerde verwijzing verwijderen", "Verwijzingen bewerken", "Annuleren" };
        if (entries.Count == 0)
        {
            var emptyAction = await DisplayActionSheet(
                _english ? "Project media library is empty" : "De mediabibliotheek is leeg",
                _english ? "Cancel" : "Annuleren", null,
                _english ? "Add a text reference" : "Tekstverwijzing toevoegen");
            if (emptyAction == (_english ? "Add a text reference" : "Tekstverwijzing toevoegen"))
                await EditMediaReferencesAsync(project);
            return;
        }

        var selectedEntry = await DisplayActionSheet(
            _english ? $"Media library • {entries.Count} entries" : $"Mediabibliotheek • {entries.Count} items",
            _english ? "Cancel" : "Annuleren", null,
            entries.Select((line, index) => $"{index + 1}. {GetMediaDisplayName(line)}").ToArray());
        var selectedIndex = entries
            .Select((line, index) => new { line, index, label = $"{index + 1}. {GetMediaDisplayName(line)}" })
            .Where(item => item.label == selectedEntry)
            .Select(item => item.index)
            .DefaultIfEmpty(-1)
            .First();
        if (selectedIndex < 0) return;

        var selectedLine = entries[selectedIndex];
        var action = await DisplayActionSheet(
            GetMediaDisplayName(selectedLine),
            _english ? "Cancel" : "Annuleren", null, actions.Take(3).ToArray());
        if (action == actions[0])
        {
            var separator = selectedLine.IndexOf(" | ", StringComparison.Ordinal);
            var path = separator >= 0 ? selectedLine[(separator + 3)..].Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = GetMediaDisplayName(selectedLine),
                        File = new ShareFile(path)
                    });
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
            await EditMediaReferencesAsync(project);
        }
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

    private async Task<string?> EditLongTextAsync(string title, string initialValue, string placeholder)
    {
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
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
                RowSpacing = 14,
                Children =
                {
                    new Label { Text = title, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    editor,
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
        Grid.SetRow((View)root.Children[2], 2);
        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(saveButton, 1);

        saveButton.Clicked += async (_, _) =>
        {
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
        var selected = await DisplayActionSheet(
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

        var checklist = await DisplayPromptAsync(
            _english ? "Production checklist" : "Productiechecklist",
            _english ? "Separate tasks with semicolons. Add [x] before a completed task." : "Scheid taken met puntkomma’s. Zet [x] voor een afgeronde taak.",
            _english ? "Save" : "Opslaan",
            _english ? "Cancel" : "Annuleren",
            maxLength: 3000,
            initialValue: project.Checklist);
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
        var projects = await _store.LoadAsync();
        RenderProjects(GetFilteredProjects(projects, e.NewTextValue));
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
