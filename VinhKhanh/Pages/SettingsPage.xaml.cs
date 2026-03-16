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

            // Đồng bộ UI theo ngôn ngữ hiện tại khi mở trang
            UpdateLanguageButtonUI(_localizationService.CurrentLanguage);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: string language })
                return;

            _localizationService.CurrentLanguage = language;
            UpdateLanguageButtonUI(language);
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
    }
}