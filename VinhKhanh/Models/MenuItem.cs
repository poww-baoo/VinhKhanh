using SQLite;

namespace VinhKhanh.Models;

[Table("MenuItems")]
public class PoiMenuItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PoiId { get; set; }

    [NotNull]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsSignature { get; set; }

    [Ignore]
    public string PriceText => Price > 0 ? $"{Price:N0}đ" : "";
}