namespace CreatorNexus.App;
public partial class App : Application
{
    public App() { InitializeComponent(); MainPage = new NavigationPage(new MainPage()) { BarBackgroundColor = Color.FromArgb("#171725"), BarTextColor = Colors.White }; }
}
