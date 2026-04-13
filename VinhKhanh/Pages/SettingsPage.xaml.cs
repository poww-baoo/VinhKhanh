using System.Globalization;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly LocalizationService _localizationService;
        private List<string> _languageCodes = new();

        public SettingsPage()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            _localizationService.LanguageChanged += OnLanguageChangedEvent;

            BuildLanguagePicker();
            UpdateUI(_localizationService.CurrentLanguage);
        }

        private void BuildLanguagePicker()
        {
            _languageCodes = _localizationService.SupportedLanguages;

            LanguagePicker.ItemsSource = _languageCodes
                .Select(code => _localizationService.GetLanguageDisplayName(code))
                .ToList();

            var currentCode = _localizationService.NormalizeLanguageCode(_localizationService.CurrentLanguage);
            var index = _languageCodes.FindIndex(x => x == currentCode);
            LanguagePicker.SelectedIndex = index >= 0 ? index : 0;
        }

        private void OnLanguagePickerChanged(object sender, EventArgs e)
        {
            if (LanguagePicker.SelectedIndex < 0 || LanguagePicker.SelectedIndex >= _languageCodes.Count)
                return;

            var selectedLanguage = _languageCodes[LanguagePicker.SelectedIndex];
            _localizationService.CurrentLanguage = selectedLanguage;
        }

        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            var currentCode = _localizationService.NormalizeLanguageCode(_localizationService.CurrentLanguage);
            var index = _languageCodes.FindIndex(x => x == currentCode);
            if (index >= 0 && LanguagePicker.SelectedIndex != index)
            {
                LanguagePicker.SelectedIndex = index;
            }

            UpdateUI(_localizationService.CurrentLanguage);
        }

        private void UpdateUI(string language)
        {
            UpdatePageTexts(language);
        }

        private void UpdatePageTexts(string language)
        {
            Title = _localizationService.GetString("Settings", language);
            HeaderLabel.Text = $"⚙️ {_localizationService.GetString("Settings", language)}";
            LanguageLabel.Text = _localizationService.GetString("Language", language);
            LanguagePicker.Title = _localizationService.GetString("SelectLanguage", language);

            NotificationsLabel.Text = _localizationService.GetString("Notifications", language);
            NotificationDescLabel.Text = _localizationService.GetString("NotificationDesc", language);

            AboutLabel.Text = _localizationService.GetString("About", language);
            AppNameLabel.Text = _localizationService.GetString("AppName", language);
            VersionLabel.Text = _localizationService.GetString("Version", language);
        }
    }
}