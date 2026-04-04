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

    public string Address { get; set; } = "";

    public string TextVi { get; set; } = "";
    public string TextEn { get; set; } = "";
    public string TextZh { get; set; } = "";

    [Column("TextJp")]
    public string TextJp { get; set; } = "";

    [Ignore]
    public string TextJa
    {
        get => TextJp;
        set => TextJp = value;
    }

    public string TextRu { get; set; } = "";
    public string TextFr { get; set; } = "";

    public double Lat { get; set; }
    public double Lng { get; set; }
    public double RadiusMeters { get; set; } = 30;
    public int Priority { get; set; } = 1;
    public int YearEstablished { get; set; }
    public double Rating { get; set; }
    public string ImageFileName { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public string GetTextByLanguage(string language)
    {
        var normalized = (language ?? "vi").Trim().ToLowerInvariant();

        var text = normalized switch
        {
            "en" => TextEn,
            "zh" => TextZh,
            "ja" => TextJp,
            "jp" => TextJp,
            "ru" => TextRu,
            "fr" => TextFr,
            _ => TextVi
        };

        return string.IsNullOrWhiteSpace(text)
            ? (string.IsNullOrWhiteSpace(TextVi) ? History : TextVi)
            : text;
    }

    [Ignore] public string RatingText => $"★ {Rating:F1}";
    [Ignore] public string YearText => $"Từ năm {YearEstablished}";
    [Ignore] public bool IsNearby { get; set; }
    [Ignore] public string DistanceText { get; set; } = "";
    [Ignore] public string CategoryName { get; set; } = "";
}