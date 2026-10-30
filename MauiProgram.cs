// using Java.Net;
using Microsoft.Extensions.Logging;
using Supabase;

namespace SupabaseMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 1. Add your Supabase Cloud Database Credentials
        var supabaseUrl = "https://cnohhqluwninqkubgxug.supabase.co";
        var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNub2hocWx1d25pbnFrdWJneHVnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTI1OTM5MTYsImV4cCI6MjEwODE2OTkxNn0.MHSUpRwoTo5rj6D9hKokMommSjOepDqObTH_7hKo5Nw";

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };

        var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
        builder.Services.AddSingleton(supabaseClient);

        // Dependency Injection Registrations
        builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<AuthPage>();
      
        builder.Services.AddTransient<MainPage>(); // 🚀 Errors on 'MainPage' will disappear

        return builder.Build();
    }
}
