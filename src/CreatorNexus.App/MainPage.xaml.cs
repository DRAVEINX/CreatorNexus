using System.Collections.ObjectModel;
using CreatorNexus.Core;
namespace CreatorNexus.App;
public partial class MainPage : ContentPage
{
    private readonly ProjectStore _store = new();
    private readonly ObservableCollection<CreatorProject> _visibleProjects = new();
    private bool _english;
    public MainPage()
    {
        InitializeComponent(); ProjectsCollection.ItemsSource = _visibleProjects;
        SizeChanged += (_, _) => ApplyResponsiveLayout(); ApplyLanguage(); _ = LoadProjectsAsync();
    }
    private async Task LoadProjectsAsync() => RenderProjects(ProjectRules.Search(await _store.LoadAsync(), ProjectSearchBar.Text));
    private void RenderProjects(IEnumerable<CreatorProject> projects)
    {
        _visibleProjects.Clear(); foreach (var project in projects) _visibleProjects.Add(project);
        ProjectCountLabel.Text = _english ? $"{_visibleProjects.Count} projects" : $"{_visibleProjects.Count} projecten";
    }
    private async void OnAddProjectClicked(object sender, EventArgs e)
    {
        var title = NewProjectEntry.Text; var error = ProjectRules.ValidateTitle(title);
        if (!string.IsNullOrEmpty(error)) { await DisplayAlert(_english ? "Check the title" : "Controleer de titel", _english ? "Enter a title of up to 120 characters." : "Vul een titel in van maximaal 120 tekens.", _english ? "OK" : "Oké"); return; }
        await _store.AddAsync(new CreatorProject { Title = title!.Trim(), Channel = _english ? "My channel" : "Mijn kanaal" });
        NewProjectEntry.Text = string.Empty; ProjectSearchBar.Text = string.Empty; await LoadProjectsAsync();
    }
    private async void OnAdvanceStatusClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id) return;
        var project = (await _store.LoadAsync()).FirstOrDefault(p => p.Id == id); if (project is null) return;
        var next = project.Status == ProjectStatus.Published ? ProjectStatus.Idea : (ProjectStatus)((int)project.Status + 1);
        await _store.UpdateAsync(project.WithStatus(next)); await LoadProjectsAsync();
    }
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e) => RenderProjects(ProjectRules.Search(await _store.LoadAsync(), e.NewTextValue));
    private void OnLanguageClicked(object sender, EventArgs e) { _english = !_english; ApplyLanguage(); }
    private void ApplyLanguage()
    {
        LanguageButton.Text = _english ? "NL" : "EN"; SubtitleLabel.Text = _english ? "Your creative command center" : "Jouw creatieve commandocentrum";
        WelcomeLabel.Text = _english ? "Turn ideas into real projects." : "Maak van ideeën echte projecten.";
        IntroLabel.Text = _english ? "Capture ideas, organize content and keep your next step in sight." : "Bewaar ideeën, organiseer content en houd je volgende stap in zicht.";
        NewProjectEntry.Placeholder = _english ? "Name your new project…" : "Naam van je nieuwe project…"; AddButton.Text = _english ? "+ New" : "+ Nieuw";
        ProjectsHeading.Text = _english ? "Your projects" : "Jouw projecten"; ProjectSearchBar.Placeholder = _english ? "Search projects…" : "Zoek projecten…";
        EmptyTitleLabel.Text = _english ? "No projects yet" : "Nog geen projecten"; EmptySubtitleLabel.Text = _english ? "Create your first creator project above." : "Maak hierboven je eerste creatorproject.";
        FooterLabel.Text = _english ? "Local storage • Offline ready • Free" : "Lokale opslag • Offline beschikbaar • Gratis";
        RenderProjects(ProjectRules.Search(_visibleProjects, ProjectSearchBar.Text));
    }
    private void ApplyResponsiveLayout()
    {
        Padding = Width >= 1000 ? new Thickness(44, 28) : Width >= 600 ? new Thickness(28, 22) : new Thickness(18);
    }
}
