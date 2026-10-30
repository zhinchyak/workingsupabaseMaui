using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SupabaseMauiApp;

namespace SupabaseMauiApp;

public class AuthViewModel : INotifyPropertyChanged
{
    private readonly Supabase.Client _supabaseClient;
    private bool _isLoginState = true;
    private bool _isBusy;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private readonly IServiceProvider _serviceProvider; // Add this reference

    public event PropertyChangedEventHandler? PropertyChanged;

    public AuthViewModel(Supabase.Client supabaseClient, IServiceProvider serviceProvider)
    {
        _supabaseClient = supabaseClient;
        _serviceProvider = serviceProvider; // Save it here

        AuthenticateCommand = new Command(async () => await ExecuteAuthAsync(), () => !IsBusy);
        ToggleStateCommand = new Command(ExecuteToggleState);
    }



    // UI Bindings
    public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
    public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
    public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); ((Command)AuthenticateCommand).ChangeCanExecute(); }
    }

    public string StateTitle => _isLoginState ? "Sign In to Your Account" : "Create a New Account";
    public string ActionButtonText => _isLoginState ? "Login" : "Register";
    public string ToggleButtonText => _isLoginState ? "Don't have an account? Sign Up" : "Already have an account? Sign In";

    public ICommand AuthenticateCommand { get; }
    public ICommand ToggleStateCommand { get; }

    private void ExecuteToggleState()
    {
        _isLoginState = !_isLoginState;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(StateTitle));
        OnPropertyChanged(nameof(ActionButtonText));
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    private async Task ExecuteAuthAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and Password fields are required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            Supabase.Gotrue.Session? session = null;

            if (_isLoginState)
            {
                session = await _supabaseClient.Auth.SignInWithPassword(Email, Password);
            }
            else
            {
                session = await _supabaseClient.Auth.SignUp(Email, Password);
            }

            if (session != null || !_isLoginState)
            {
                if (!_isLoginState && session == null)
                {
                    ErrorMessage = "Registration successful! Please check your email for a confirmation link.";
                    return;
                }

                // FIXED LINE: Use the injected _serviceProvider to resolve MainPage safely
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var mainPage = _serviceProvider.GetRequiredService<MainPage>();

                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new NavigationPage(mainPage);

                        // 🚀 THE CRITICAL FIX: Trigger authorization and open the websocket channel 
                        // now that the session is officially verified!
                        await mainPage.InitializeRealtimeAsync();
                    }
                });


            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }

    }

    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
