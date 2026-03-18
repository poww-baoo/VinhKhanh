using SQLite;

namespace VinhKhanh.Models;

[Table("PlaybackLogs")]
public class PlaybackLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PoiId { get; set; }
    public string Language { get; set; } = "vi";
    public DateTime PlayedAt { get; set; } = DateTime.Now;
    public string DeviceId { get; set; } = "";
}