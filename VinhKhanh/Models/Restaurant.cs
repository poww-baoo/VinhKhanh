namespace VinhKhanh.Models
{
    public class Restaurant
    {
        public string Id { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int YearEstablished { get; set; }

        public string History { get; set; } = string.Empty;
        public string HistoryEn { get; set; } = string.Empty;
        public string HistoryJp { get; set; } = string.Empty;
        public string HistoryZh { get; set; } = string.Empty;
        public string HistoryRu { get; set; } = string.Empty;
        public string HistoryFr { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string AdrEn { get; set; } = string.Empty;
        public string AdrJp { get; set; } = string.Empty;
        public string AdrZh { get; set; } = string.Empty;
        public string AdrRu { get; set; } = string.Empty;
        public string AdrFr { get; set; } = string.Empty;

        public string TextVi { get; set; } = string.Empty;
        public string TextEn { get; set; } = string.Empty;
        public string TextZh { get; set; } = string.Empty;
        public string TextJa { get; set; } = string.Empty;
        public string TextRu { get; set; } = string.Empty;
        public string TextFr { get; set; } = string.Empty;
        public string Highlights { get; set; } = string.Empty;
        public string DisplayAddress { get; set; } = string.Empty;
        public string DisplayHighlights { get; set; } = string.Empty;
        public double Rating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GeofenceRadius { get; set; } = 100;
        public int Priority { get; set; }
        public string ImageFileName { get; set; } = string.Empty;
        public string DisplayImage { get; set; } = "placeholder.png";

        public SignatureDish? SignatureDish { get; set; }
        public List<RestaurantMenuItem>? HighlightMenuItems { get; set; }
        public List<Promotion>? Promotions { get; set; }
        public Dictionary<string, RestaurantLocalized>? LocalizedContent { get; set; }

        public string GetHistoryByLanguage(string language)
        {
            var normalized = (language ?? "vi").Trim().ToLowerInvariant();

            var history = normalized switch
            {
                "en" => HistoryEn,
                "ja" => HistoryJp,
                "jp" => HistoryJp,
                "zh" => HistoryZh,
                "ru" => HistoryRu,
                "fr" => HistoryFr,
                _ => History
            };

            return string.IsNullOrWhiteSpace(history) ? History : history;
        }

        public string GetAddressByLanguage(string language)
        {
            var normalized = (language ?? "vi").Trim().ToLowerInvariant();

            var address = normalized switch
            {
                "en" => AdrEn,
                "ja" => AdrJp,
                "jp" => AdrJp,
                "zh" => AdrZh,
                "ru" => AdrRu,
                "fr" => AdrFr,
                _ => Address
            };

            return string.IsNullOrWhiteSpace(address) ? Address : address;
        }

        public string GetTextByLanguage(string language)
        {
            var normalized = (language ?? "vi").Trim().ToLowerInvariant();

            var text = normalized switch
            {
                "en" => TextEn,
                "zh" => TextZh,
                "ja" => TextJa,
                "ru" => TextRu,
                "fr" => TextFr,
                _ => TextVi
            };

            return string.IsNullOrWhiteSpace(text)
                ? (string.IsNullOrWhiteSpace(History) ? string.Empty : History)
                : text;
        }
    }

    public class SignatureDish
    {
        public Dictionary<string, string> Names { get; set; } = new();
        public Dictionary<string, string> Stories { get; set; } = new();
        public Dictionary<string, string> Reasons { get; set; } = new();
    }

    public class RestaurantMenuItem
    {
        public Dictionary<string, string> Names { get; set; } = new();
        public Dictionary<string, string> Descriptions { get; set; } = new();
    }

    public class Promotion
    {
        public Dictionary<string, string> Titles { get; set; } = new();
        public Dictionary<string, string> Descriptions { get; set; } = new();
    }

    public class RestaurantLocalized
    {
        public string History { get; set; } = string.Empty;
        public string Highlights { get; set; } = string.Empty;
    }
}