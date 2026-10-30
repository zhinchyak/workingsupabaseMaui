using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace SupabaseMauiApp.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        // 🛠️ Windows initialization layer completely separated from the old root XAML
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
