using static Supabase.Realtime.Constants;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Supabase.Realtime;
using Supabase.Realtime.Models;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

using Label = Microsoft.Maui.Controls.Label;
using DataTemplate = Microsoft.Maui.Controls.DataTemplate;
using MauiBinding = Microsoft.Maui.Controls.Binding;

namespace SupabaseMauiApp;

#pragma warning disable CS0618 
#pragma warning disable CS8622 

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private RealtimeChannel? _realtimeChannel;

    private Entry _taskNameInput;
    private Entry _categoryInput;
    private Button _submitBtn;
    private Label _titleLabel;
    private CollectionView _tasksCollectionView;

    // 🚀 CRITICAL FIX 1: Explicitly initialize and leave this instance unmodified 
    // to protect your visual layout binding wires!
    public ObservableCollection<TaskModel> TodoList { get; } = new();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Instantly pull down existing tasks from the cloud database
        await LoadInitialTasksFromSupabaseAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await DisconnectRealtimeAsync();
    }

    private async Task LoadInitialTasksFromSupabaseAsync()
    {
        try
        {
            var response = await _supabaseClient.From<TaskModel>().Get();
            var baselineTasks = response.Models;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Clear items one by one inside the collection instead of re-instantiating the whole array!
                TodoList.Clear();
                foreach (var task in baselineTasks)
                {
                    TodoList.Add(task);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading initial tasks: {ex.Message}");
        }
    }

    public async Task InitializeRealtimeAsync()
    {
        try
        {
            var currentSession = _supabaseClient.Auth.CurrentSession;

            if (currentSession?.AccessToken != null)
            {
                _supabaseClient.Realtime.SetAuth(currentSession.AccessToken);
            }

            await _supabaseClient.Realtime.ConnectAsync();

            _realtimeChannel = (RealtimeChannel)_supabaseClient.Realtime.Channel("todo-items-stream");

            _realtimeChannel.OnPostgresChange((sender, change) =>
            {
                HandleDatabaseChanges(sender, change);
            }, PostgresChangesOptions.ListenType.All, new PostgresChangesFilter { Schema = "public", Table = "todo_items" });

            await _realtimeChannel.Subscribe();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Realtime setup error: {ex.Message}");
        }
    }

    private void HandleDatabaseChanges(object sender, PostgresChangesResponse change)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool collectionChanged = false;

            switch (change.Event)
            {
                case EventType.Insert:
                    var newRecord = change.Model<TaskModel>();
                    if (newRecord != null && !TodoList.Any(t => t.Id == newRecord.Id))
                    {
                        TodoList.Add(newRecord);
                        collectionChanged = true; // 🚀 Flag an insert action
                    }
                    break;

                case EventType.Update:
                    var updatedRecord = change.Model<TaskModel>();
                    if (updatedRecord != null)
                    {
                        var existingUpdate = TodoList.FirstOrDefault(t => t.Id == updatedRecord.Id);
                        if (existingUpdate != null)
                        {
                            existingUpdate.task_name = updatedRecord.task_name;
                            existingUpdate.category = updatedRecord.category;
                            existingUpdate.is_completed = updatedRecord.is_completed;
                            collectionChanged = true;
                        }
                    }
                    break;

                case EventType.Delete:
                    var deletedRecord = change.OldModel<TaskModel>();
                    if (deletedRecord != null)
                    {
                        var existingDelete = TodoList.FirstOrDefault(t => t.Id == deletedRecord.Id);
                        if (existingDelete != null)
                        {
                            TodoList.Remove(existingDelete);
                            collectionChanged = true; // 🚀 Flag a deletion action
                        }
                    }
                    break;
            }

            // 🚀 THE NATIVE MAUI FORCED REPAINT TRICK:
            // If a real-time socket packet altered the collection, we reset the layout link.
            // This strips away stale cache states and forces the screen to mirror changes immediately.
            if (collectionChanged && _tasksCollectionView != null)
            {
                _tasksCollectionView.ItemsSource = null;
                _tasksCollectionView.ItemsSource = TodoList;
            }
        });
    }
    public async Task DisconnectRealtimeAsync()
    {
        if (_realtimeChannel != null)
        {
            _realtimeChannel.Unsubscribe();
            _realtimeChannel = null;
        }
        await Task.CompletedTask;
    }

    public MainPage(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
        BindingContext = this;

        _titleLabel = new Label
        {
            Text = "Todo Items",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10),
            TextColor = Colors.Black
        };

        _taskNameInput = new Entry { Placeholder = "Enter task name...", WidthRequest = 300, HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Black, PlaceholderColor = Colors.Gray };
        _categoryInput = new Entry { Placeholder = "Enter category...", WidthRequest = 300, HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Black, PlaceholderColor = Colors.Gray };

        _submitBtn = new Button { Text = "Save to Supabase Cloud", HeightRequest = 50, WidthRequest = 250, HorizontalOptions = LayoutOptions.Center };
        _submitBtn.Clicked += OnSubmitButtonClicked;

        var itemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                Padding = new Thickness(15),
                BackgroundColor = Colors.WhiteSmoke,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 30 }
                }
            };

            var completedCheckBox = new CheckBox { VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 10, 0) };
            completedCheckBox.SetBinding(CheckBox.IsCheckedProperty, nameof(TaskModel.is_completed));
            completedCheckBox.CheckedChanged += async (sender, args) =>
            {
                if (sender is CheckBox checkBox && checkBox.BindingContext is TaskModel clickedItem)
                {
                    clickedItem.is_completed = args.Value;
                    await UpdateItemInSupabase(clickedItem);
                }
            };

            var nameLabel = new Label { FontAttributes = FontAttributes.Bold, TextColor = Colors.Black };
            nameLabel.SetBinding(Label.TextProperty, nameof(TaskModel.task_name));

            var categoryLabel = new Label { FontSize = 12, TextColor = Colors.DimGray };
            categoryLabel.SetBinding(Label.TextProperty, nameof(TaskModel.category));

            var dateLabel = new Label { FontSize = 10, TextColor = Colors.DarkGray, Margin = new Thickness(0, 4, 0, 0) };
            dateLabel.SetBinding(Label.TextProperty, new MauiBinding(nameof(TaskModel.created_at), stringFormat: "{0:MM/dd/yyyy HH:mm}"));

            var textStack = new VerticalStackLayout { Children = { nameLabel, categoryLabel, dateLabel }, VerticalOptions = LayoutOptions.Center };

            grid.Add(completedCheckBox, 0, 0);
            grid.Add(textStack, 1, 0);

            var deleteAction = new SwipeItem
            {
                Text = "Delete",
                BackgroundColor = Color.FromArgb("#B71C1C")
            };

            deleteAction.Invoked += async (sender, args) =>
            {
                if (sender is SwipeItem visualItem && visualItem.BindingContext is TaskModel itemToDelete)
                {
                    bool confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{itemToDelete.task_name}'?", "Delete", "Cancel");
                    if (!confirm) return;

                    // 🚀 CRITICAL FIX 2: OPTIMISTIC UI REMOVAL
                    // Instantly drop it from your screen array right now so it vanishes live!
                    TodoList.Remove(itemToDelete);

                    await DeleteItemFromSupabase(itemToDelete);
                }
            };

            return new SwipeView
            {
                RightItems = new SwipeItems { deleteAction },
                Content = grid
            };
        });

        _tasksCollectionView = new CollectionView
        {
            ItemTemplate = itemTemplate,
            ItemsSource = TodoList, // Securely bound to our persistent collection memory
            Margin = new Thickness(10)
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Padding = new Thickness(20),
                Children = { _titleLabel, _taskNameInput, _categoryInput, _submitBtn, _tasksCollectionView }
            }
        };
    }
    private async void OnSubmitButtonClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_taskNameInput.Text)) return;

        var currentUser = _supabaseClient.Auth.CurrentUser;
        if (currentUser == null) return;

        var newTask = new TaskModel
        {
            task_name = _taskNameInput.Text,
            category = _categoryInput.Text ?? "General",
            UserId = Guid.Parse(currentUser.Id),
            created_at = DateTimeOffset.UtcNow,
            is_completed = false
        };

        // 1. Instantly drop it on screen (Optimistic UI starts with Id = 0)
        TodoList.Add(newTask);

        _taskNameInput.Text = string.Empty;
        _categoryInput.Text = string.Empty;

        try
        {
            // 🚀 THE FIX: Capture the server response payload!
            var response = await _supabaseClient.From<TaskModel>().Insert(newTask);

            // Extract the newly generated record row sent back by Postgres
            var savedRecord = response.Models.FirstOrDefault();

            if (savedRecord != null)
            {
                // Update your local task object with its actual database Primary Key ID!
                newTask.Id = savedRecord.Id;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed insertion fallback: {ex.Message}");
        }
    }



    private async Task UpdateItemInSupabase(TaskModel item)
    {
        await _supabaseClient.From<TaskModel>().Update(item);
    }
    private async Task DeleteItemFromSupabase(TaskModel item)
    {
        await _supabaseClient.From<TaskModel>().Delete(item);
    }
}