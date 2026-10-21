using System;
using System.Reflection.Emit;
using Supabase;

namespace SupabaseMauiApp;

public class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private Button _submitBtn;
    private Label _titleLabel;

    // The Supabase client is automatically injected here by MauiProgram
    public MainPage(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;

        // 1. Create the Title Label in C#
        _titleLabel = new Label
        {
            Text = "Supabase Cloud Test (C# UI)",
            FontSize = 32,
            HorizontalOptions = LayoutOptions.Center
        };

        // 2. Create the Button in C#
        _submitBtn = new Button
        {
            Text = "Send Data to Supabase",
            HeightRequest = 50,
            WidthRequest = 250,
            HorizontalOptions = LayoutOptions.Center
        };

        // 3. Attach the Click Event Handler
        _submitBtn.Clicked += OnSubmitButtonClicked;

        // 4. Arrange them vertically on the screen
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 20,
            Children = { _titleLabel, _submitBtn }
        };
    }

    private async void OnSubmitButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Create a test row matching your C# model structure
            var testTask = new TaskModel
            {
                Title = "Successfully sent from a pure C# UI on Windows!"
            };

            // Push the item straight to your cloud database table
            await _supabaseClient.From<TaskModel>().Insert(testTask);

            // Show a visual alert on your screen confirming it worked
            await DisplayAlert("Success!", "Data uploaded to Supabase Cloud!", "Awesome");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Database sync failed: {ex.Message}", "OK");
        }
    }
}
