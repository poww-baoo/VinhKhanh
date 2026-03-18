using SQLite;

namespace VinhKhanh.Models;

[Table("TranslationCache")]
public class TranslationCache
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PoiId { get; set; }
    public string Language { get; set; } = "en";
    public string TranslatedText { get; set; } = "";
    public DateTime CachedAt { get; set; } = DateTime.Now;

    [Ignore]
    public bool IsValid =>
        DateTime.Now - CachedAt < TimeSpan.FromDays(30);
}