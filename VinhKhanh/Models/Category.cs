using SQLite;

namespace VinhKhanh.Models;

[Table("Categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = "";

    public string IconText { get; set; } = "";
    public int SortOrder { get; set; } = 0;

    [Ignore]
    public bool IsSelected { get; set; }
}