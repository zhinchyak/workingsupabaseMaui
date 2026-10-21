using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SupabaseMauiApp;

[Table("todo_items")]
public class TaskModel : BaseModel
{
    public TaskModel() { }

    [PrimaryKey("id", false)]
    [JsonProperty("id")]
    public long Id { get; set; }

    // This forces the C# compiler to serialize your text as "task_name"
    [Column("task_name")]
    [JsonProperty("task_name")]
    public string TaskName { get; set; } = string.Empty;

    // This forces the C# compiler to serialize your text as "category"
    [Column("category")]
    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;
}

