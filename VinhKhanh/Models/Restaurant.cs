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

        public string Address { get; set; } = string.Empty;
        public string AdrEn { get; set; } = string.Empty;

        public string TextVi { get; set; } = string.Empty;
        public string TextEn { get; set; } = string.Empty;
        public string TextZh { get; set; } = string.Empty;
        public string TextJa { get; set; } = string.Empty;
        public string TextRu { get; set; } = string.Empty;
        public string TextFr { get; set; } = string.Empty;
        public string Highlights { get; set; } = string.Empty;
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
            return normalized == "en" && !string.IsNullOrWhiteSpace(HistoryEn)
                ? HistoryEn
                : History;
        }

        public string GetAddressByLanguage(string language)
        {
            var normalized = (language ?? "vi").Trim().ToLowerInvariant();
            return normalized == "en" && !string.IsNullOrWhiteSpace(AdrEn)
                ? AdrEn
                : Address;
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
                ? (string.IsNullOrWhiteSpace(TextVi) ? History : TextVi)
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