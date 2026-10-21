using Supabase;
using System.Net.Http;
using System.Text;


namespace SupabaseMauiApp;

public class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;

    // Define the interactive UI inputs
    private Entry _taskNameInput;
    private Entry _categoryInput;
    private Button _submitBtn;
    private Label _titleLabel;

    public MainPage(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;

        _titleLabel = new Label
        {
            Text = "Supabase Todo Entry",
            FontSize = 28,
            HorizontalOptions = LayoutOptions.Center
        };

        // 1. Text input for Task Name
        _taskNameInput = new Entry
        {
            Placeholder = "Enter task name here...",
            WidthRequest = 300,
            HorizontalOptions = LayoutOptions.Center
        };

        // 2. Text input for Category
        _categoryInput = new Entry
        {
            Placeholder = "Enter category (e.g., Work, Home)...",
            WidthRequest = 300,
            HorizontalOptions = LayoutOptions.Center
        };

        _submitBtn = new Button
        {
            Text = "Save to Supabase Cloud",
            HeightRequest = 50,
            WidthRequest = 250,
            HorizontalOptions = LayoutOptions.Center
        };

        _submitBtn.Clicked += OnSubmitButtonClicked;

        // Arrange them all down the screen
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 20,
            Children = { _titleLabel, _taskNameInput, _categoryInput, _submitBtn }
        };
    }

    private async void OnSubmitButtonClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_taskNameInput.Text) || string.IsNullOrWhiteSpace(_categoryInput.Text))
        {
            await DisplayAlert("Missing Info", "Please fill out both text boxes first!", "OK");
            return;
        }

        try
        {
            // 1. Manually build a raw JSON string to guarantee it's never empty
            string rawJson = $"{{\"task_name\": \"{_taskNameInput.Text}\", \"category\": \"{_categoryInput.Text}\"}}";

            // 2. Fetch the credentials we stored in MauiProgram.cs
            var supabaseUrl = "https://cnohhqluwninqkubgxug.supabase.co"; // Paste your real URL here
            var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNub2hocWx1d25pbnFrdWJneHVnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTI1OTM5MTYsImV4cCI6MjEwODE2OTkxNn0.MHSUpRwoTo5rj6D9hKokMommSjOepDqObTH_7hKo5Nw";        // Paste your real Anon Key here

            // 3. Fire a direct web request straight to your PostgREST database endpoint
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

                var content = new StringContent(rawJson, Encoding.UTF8, "application/json");

                // PostgREST expects a table endpoint at /rest/v1/TABLENAME
                var response = await client.PostAsync($"{supabaseUrl}/rest/v1/todo_items", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success!", "Bypassed the client! Data uploaded to Supabase Cloud!", "Awesome");
                    _taskNameInput.Text = string.Empty;
                    _categoryInput.Text = string.Empty;
                }
                else
                {
                    string apiError = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Server Error", $"Status: {response.StatusCode}\nDetails: {apiError}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network Exception", ex.Message, "OK");
        }
    }



}
