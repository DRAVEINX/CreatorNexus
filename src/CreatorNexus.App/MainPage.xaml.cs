using System.Collections.ObjectModel;
using CreatorNexus.Core;

namespace CreatorNexus.App;

public partial class MainPage : ContentPage
{
    private readonly ProjectStore _store = new();
    private readonly ObservableCollection<CreatorProject> _visibleProjects = new();
    private bool _english;
    private bool _showArchived;

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
        RenderProjects(ProjectRules.Search(
            projects.Where(p => p.IsArchived == _showArchived),
            ProjectSearchBar.Text));
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
        RenderProjects(ProjectRules.Search(
            projects.Where(p => p.IsArchived == _showArchived),
            e.NewTextValue));
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
        ProjectsHeading.Text = _english ? "Your projects" : "Jouw projecten";
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
