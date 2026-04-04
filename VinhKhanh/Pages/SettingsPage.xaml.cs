using System.Globalization;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly LocalizationService _localizationService;

        public SettingsPage()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            _localizationService.LanguageChanged += OnLanguageChangedEvent;

            UpdateUI(_localizationService.CurrentLanguage);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: string language })
                return;

            _localizationService.CurrentLanguage = language;
        }

        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            UpdateUI(_localizationService.CurrentLanguage);
        }

        private void UpdateUI(string language)
        {
            UpdateLanguageButtonUI(language);
            UpdatePageTexts(language);
        }

        private void UpdateLanguageButtonUI(string selectedLanguage)
        {
            if (selectedLanguage == "vi")
            {
                ViLanguageButton.BackgroundColor = Color.FromArgb("#FF6B35");
                ViLanguageButton.TextColor = Colors.White;
                EnLanguageButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                EnLanguageButton.TextColor = Color.FromArgb("#1F1F1F");
            }
            else
            {
                EnLanguageButton.BackgroundColor = Color.FromArgb("#FF6B35");
                EnLanguageButton.TextColor = Colors.White;
                ViLanguageButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                ViLanguageButton.TextColor = Color.FromArgb("#1F1F1F");
            }
        }

        private void UpdatePageTexts(string language)
        {
            Title = _localizationService.GetString("Settings", language);
            HeaderLabel.Text = $"⚙️ {_localizationService.GetString("Settings", language)}";
            LanguageLabel.Text = _localizationService.GetString("Language", language);
            NotificationsLabel.Text = _localizationService.GetString("Notifications", language);
            NotificationDescLabel.Text = _localizationService.GetString("NotificationDesc", language);
            AboutLabel.Text = _localizationService.GetString("About", language);
            AppNameLabel.Text = _localizationService.GetString("AppName", language);
            VersionLabel.Text = _localizationService.GetString("Version", language);
        }
    }
}