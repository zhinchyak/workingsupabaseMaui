using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace SupabaseMauiApp;

[Table("todo_items")]
public class TaskModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("task_name")]
    public string task_name { get; set; } = null!;

    [Column("category")]
    public string category { get; set; } = null!;

    // FIX: Change 'CreatedAt' to lowercase 'created_at' 
    // This forces the JSON deserializer to catch the server's timestamp value!
    [Column("created_at")]
    public DateTimeOffset created_at { get; set; }

    [Column("is_completed")]
    public bool is_completed { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

}
