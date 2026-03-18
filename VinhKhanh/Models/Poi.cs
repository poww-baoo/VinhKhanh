using SQLite;

namespace VinhKhanh.Models;

[Table("Pois")]
public class Poi
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CategoryId { get; set; }          // FK → Categories

    [NotNull, MaxLength(200)]
    public string Name { get; set; } = "";

    public string History { get; set; } = "";

    // Chỉ lưu tiếng Việt — tiếng Anh dịch qua API
    public string TextVi { get; set; } = "";

    public double Lat { get; set; }
    public double Lng { get; set; }
    public double RadiusMeters { get; set; } = 30;
    public int Priority { get; set; } = 1;
    public int YearEstablished { get; set; }
    public double Rating { get; set; }
    public string ImageFileName { get; set; } = "";
    public bool IsActive { get; set; } = true;

    [Ignore] public string RatingText => $"★ {Rating:F1}";
    [Ignore] public string YearText => $"Từ năm {YearEstablished}";
    [Ignore] public bool IsNearby { get; set; }
    [Ignore] public string DistanceText { get; set; } = "";
    [Ignore] public string CategoryName { get; set; } = "";
}