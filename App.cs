using Microsoft.Maui.Controls;

namespace SupabaseMauiApp;

public class App : Application
{
    private readonly AuthPage _authPage;

    // The Dependency Injection container automatically provides your C# AuthPage here
    public App(AuthPage authPage)
    {
        _authPage = authPage;
    }

    // Modern .NET MAUI window entry layer (No XAML or InitializeComponent needed)
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_authPage));
    }
}
