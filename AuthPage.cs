using Microsoft.Maui.Controls;

namespace SupabaseMauiApp;

public partial class AuthPage : ContentPage
{
    public AuthPage(AuthViewModel viewModel)
    {
        BindingContext = viewModel;
        Title = "Welcome";

        // Layout Container
        var rootLayout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = new Thickness(30),
            WidthRequest = 400,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        // Title 
        var headerLabel = new Label
        {
            Text = "🚀 Supabase Tasks",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        rootLayout.Children.Add(headerLabel);

        // State Title Label
        var stateTitleLabel = new Label
        {
            FontSize = 20,
            HorizontalOptions = LayoutOptions.Center
        };
        stateTitleLabel.SetBinding(Label.TextProperty, nameof(AuthViewModel.StateTitle));
        rootLayout.Children.Add(stateTitleLabel);

        // Email Entry
        var emailEntry = new Entry
        {
            Placeholder = "Email Address",
            Keyboard = Keyboard.Email,
            HeightRequest = 50
        };
        emailEntry.SetBinding(Entry.TextProperty, nameof(AuthViewModel.Email));
        rootLayout.Children.Add(emailEntry);

        // Password Entry
        var passwordEntry = new Entry
        {
            Placeholder = "Password",
            IsPassword = true,
            HeightRequest = 50
        };
        passwordEntry.SetBinding(Entry.TextProperty, nameof(AuthViewModel.Password));
        rootLayout.Children.Add(passwordEntry);

        // Main Auth Action Button
        var actionButton = new Button
        {
            HeightRequest = 50,
            Margin = new Thickness(0, 10, 0, 0)
        };
        actionButton.SetBinding(Button.TextProperty, nameof(AuthViewModel.ActionButtonText));
        actionButton.SetBinding(Button.CommandProperty, nameof(AuthViewModel.AuthenticateCommand));
        rootLayout.Children.Add(actionButton);

        // State Toggle Button
        var toggleButton = new Button
        {
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Blue
        };
        toggleButton.SetBinding(Button.TextProperty, nameof(AuthViewModel.ToggleButtonText));
        toggleButton.SetBinding(Button.CommandProperty, nameof(AuthViewModel.ToggleStateCommand));
        rootLayout.Children.Add(toggleButton);

        // Error Message Banner
        var errorLabel = new Label
        {
            TextColor = Colors.Red,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        errorLabel.SetBinding(Label.TextProperty, nameof(AuthViewModel.ErrorMessage));
        errorLabel.SetBinding(Label.IsVisibleProperty, nameof(AuthViewModel.HasError));
        rootLayout.Children.Add(errorLabel);

        // Loading Progress Spinner
        var loadingIndicator = new ActivityIndicator
        {
            Color = Colors.Blue,
            HorizontalOptions = LayoutOptions.Center
        };
        loadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(AuthViewModel.IsBusy));
        loadingIndicator.SetBinding(ActivityIndicator.IsVisibleProperty, nameof(AuthViewModel.IsBusy));
        rootLayout.Children.Add(loadingIndicator);

        // Attach layout to the scroll view wrapper
        Content = new ScrollView { Content = rootLayout };
    }
}
