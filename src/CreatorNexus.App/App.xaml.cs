namespace CreatorNexus.App;

public partial class App : Application
{
    public App() => InitializeComponent();

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigationPage = new NavigationPage(new MainPage())
        {
            BarBackgroundColor = Color.FromArgb("#171725"),
            BarTextColor = Colors.White
        };

        return new Window(navigationPage);
    }
}
