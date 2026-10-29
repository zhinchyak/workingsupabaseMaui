using System.Collections.ObjectModel;
using Supabase;
using Supabase.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using DataTemplate = Microsoft.Maui.Controls.DataTemplate;
// Explicitly bind layout elements to .NET MAUI controls to prevent layout conflicts
using Label = Microsoft.Maui.Controls.Label;

namespace SupabaseMauiApp;

#pragma warning disable CS0618 
#pragma warning disable CS8622 

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;

    private Entry _taskNameInput;
    private Entry _categoryInput;
    private Button _submitBtn;
    private Label _titleLabel;
    private CollectionView _tasksCollectionView;

    public ObservableCollection<TaskModel> TodoList { get; set; } = new();

    public MainPage(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
        BindingContext = this;

        _titleLabel = new Label
        {
            Text = "Supabase Todo Items",
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
            // 1. Define your clean visible row layout grid (3 columns: CheckBox, Text Layouts, Empty Spacer)
            var grid = new Grid
            {
                Padding = new Thickness(15),
                BackgroundColor = Colors.WhiteSmoke,
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = 30 } // Tiny spacer on the right where the button used to sit
        }
            };

            // CheckBox Toggle
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

            // Stacked Text Labels (Task Name, Category, and Real Timestamp)
            var nameLabel = new Label { FontAttributes = FontAttributes.Bold, TextColor = Colors.Black };
            nameLabel.SetBinding(Label.TextProperty, nameof(TaskModel.task_name));

            var categoryLabel = new Label { FontSize = 12, TextColor = Colors.DimGray };
            categoryLabel.SetBinding(Label.TextProperty, nameof(TaskModel.category));

            var dateLabel = new Label { FontSize = 10, TextColor = Colors.DarkGray, Margin = new Thickness(0, 4, 0, 0) };
            dateLabel.SetBinding(Label.TextProperty, new Binding(nameof(TaskModel.created_at), stringFormat: "{0:MM/dd/yyyy HH:mm}"));

            var textStack = new VerticalStackLayout { Children = { nameLabel, categoryLabel, dateLabel }, VerticalOptions = LayoutOptions.Center };

            // Add elements to the visible grid
            grid.Add(completedCheckBox, 0, 0);
            grid.Add(textStack, 1, 0);

            // 2. CREATE A SUBTLE REVEAL SWIPE ITEM
            var deleteAction = new SwipeItem
            {
                Text = "Delete",
                BackgroundColor = Color.FromArgb("#B71C1C"), // A rich, sophisticated dark crimson instead of bright red
               // TextColor = Colors.White
            };

            deleteAction.Invoked += async (sender, args) =>
            {
                // The most reliable way in C# to get a SwipeItem's data item inside a template
                // is to check the explicit BindingContext of the sender object itself.
                if (sender is SwipeItem visualItem && visualItem.BindingContext is TaskModel itemToDelete)
                {
                    bool confirm = await DisplayAlert("Confirm Delete",
                        $"Are you sure you want to delete '{itemToDelete.task_name}'?",
                        "Delete", "Cancel");

                    if (!confirm) return;

                    await DeleteItemFromSupabase(itemToDelete);
                }
                else
                {
                    // EMERGENCY FALLBACK: If the framework fails to pass the binding down,
                    // we can grab it directly by referencing the current visual item's target
                    var fallbackItem = (sender as BindableObject)?.BindingContext as TaskModel;
                    if (fallbackItem != null)
                    {
                        bool confirm = await DisplayAlert("Confirm Delete", $"Are you sure?", "Delete", "Cancel");
                        if (confirm) await DeleteItemFromSupabase(fallbackItem);
                    }
                }
            };


            var rightSwipeItems = new SwipeItems { deleteAction };

            // 3. RETURN THE SWIPEVIEW
            return new SwipeView
            {
                RightItems = rightSwipeItems,
                Content = grid
            };
        });


        _tasksCollectionView = new CollectionView
        {
            ItemTemplate = itemTemplate,
            Margin = new Thickness(10, 20, 10, 10),
            VerticalOptions = LayoutOptions.Fill,
            HeightRequest = 450,
            EmptyView = "No records found in table."
        };
        _tasksCollectionView.SetBinding(CollectionView.ItemsSourceProperty, nameof(TodoList));

        var formContainerLayout = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 12,
            Children = { _titleLabel, _taskNameInput, _categoryInput, _submitBtn, _tasksCollectionView }
        };

        Content = new ScrollView { Content = formContainerLayout };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await FetchDataFromSupabase();
    }

    private async Task FetchDataFromSupabase()
    {
        try
        {
            var response = await _supabaseClient.From<TaskModel>().Get();

            TodoList.Clear();
            if (response.Models != null && response.Models.Count > 0)
            {
                foreach (var item in response.Models)
                {
                    TodoList.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fetch Error", ex.Message, "OK");
        }
    }

    private async Task DeleteItemFromSupabase(TaskModel itemToDelete)
    {
        try
        {
            // Fix: Use a specific ID filter statement instead of passing the model object directly.
            // This guarantees PostgREST targets the exact row index on the server!
            await _supabaseClient.From<TaskModel>()
                                 .Where(x => x.Id == itemToDelete.Id)
                                 .Delete();

            // Safely bounce back to the main UI thread to update your local list view
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TodoList.Remove(itemToDelete);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Delete Error", ex.Message, "OK");
        }
    }

    private async Task UpdateItemInSupabase(TaskModel itemToUpdate)
    {
        try
        {
            // This fires a targeted UPDATE query matching the row ID behind the scenes natively
            await _supabaseClient.From<TaskModel>().Update(itemToUpdate);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Update Error", ex.Message, "OK");
        }
    }


    private async void OnSubmitButtonClicked(object? sender, System.EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_taskNameInput.Text) || string.IsNullOrWhiteSpace(_categoryInput.Text))
        {
            await DisplayAlert("Missing Info", "Please fill out both text boxes first!", "OK");
            return;
        }

        try
        {
            // Create your clean TaskModel instance (using the = null!; initializers we setup earlier)
            var newTask = new TaskModel
            {
                task_name = _taskNameInput.Text,
                category = _categoryInput.Text
            };

            // Use the injected _supabaseClient instance to execute the insert operation natively
            await _supabaseClient.From<TaskModel>().Insert(newTask);

            _taskNameInput.Text = string.Empty;
            _categoryInput.Text = string.Empty;

            await FetchDataFromSupabase();
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException pgEx)
        {
            await DisplayAlert("Database Rejection", $"Code: {pgEx.Reason}\nMessage: {pgEx.Message}", "OK");
        }
        catch (System.Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}




