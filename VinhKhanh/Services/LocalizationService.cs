using System.Globalization;

namespace VinhKhanh.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private string _currentLanguage = "vi";

        public event EventHandler? LanguageChanged;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    CultureInfo.CurrentUICulture = new CultureInfo(value);
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string GetString(string key, string? language = null)
        {
            language ??= CurrentLanguage;
            return key;
        }

        public List<string> SupportedLanguages => new() { "vi", "en", "zh", "ja", "ko" };
    }
}