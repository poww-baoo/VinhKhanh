namespace VinhKhanh.Models
{
    public class Restaurant
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int YearEstablished { get; set; }
        public string History { get; set; }
        public string Highlights { get; set; }
        public double Rating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GeofenceRadius { get; set; } = 20; // meters
        public int Priority { get; set; }

        // Modular Audio Content
        public SignatureDish SignatureDish { get; set; }
        public List<MenuItem> HighlightMenuItems { get; set; }
        public List<Promotion> Promotions { get; set; }

        // Localization
        public Dictionary<string, RestaurantLocalized> LocalizedContent { get; set; }
    }

    public class SignatureDish
    {
        public Dictionary<string, string> Names { get; set; } // { "vi": "...", "en": "..." }
        public Dictionary<string, string> Stories { get; set; }
        public Dictionary<string, string> Reasons { get; set; }
    }

    public class MenuItem
    {
        public Dictionary<string, string> Names { get; set; }
        public Dictionary<string, string> Descriptions { get; set; }
    }

    public class Promotion
    {
        public Dictionary<string, string> Titles { get; set; }
        public Dictionary<string, string> Descriptions { get; set; }
    }

    public class RestaurantLocalized
    {
        public string History { get; set; }
        public string Highlights { get; set; }
    }

    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}