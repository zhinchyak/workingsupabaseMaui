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

        // 2. Register the Supabase Client as a Singleton
        builder.Services.AddSingleton(provider =>
            new Supabase.Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoConnectRealtime = true
            }));

        // 3. Register your MainPage so it can receive the database client
        builder.Services.AddTransient<MainPage>(provider =>
       new MainPage(provider.GetRequiredService<Supabase.Client>()));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
