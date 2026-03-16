using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private LocalizationService _localizationService;

        public SettingsPage()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.CommandParameter is string language)
            {
                _localizationService.CurrentLanguage = language;
                UpdateLanguageButtonUI(language);
            }
        }

        private void UpdateLanguageButtonUI(string selectedLanguage)
        {
            var viButton = ((HorizontalStackLayout)((VerticalStackLayout)((Frame)((ScrollView)FindByName("")).Content).Content).Children[0]).Children[0] as Button;
            var enButton = ((HorizontalStackLayout)((VerticalStackLayout)((Frame)((ScrollView)FindByName("")).Content).Content).Children[0]).Children[1] as Button;

            if (viButton != null && enButton != null)
            {
                if (selectedLanguage == "vi")
                {
                    viButton.BackgroundColor = Color.FromArgb("#FF6B35");
                    viButton.TextColor = Colors.White;
                    enButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                    enButton.TextColor = Color.FromArgb("#1F1F1F");
                }
                else
                {
                    enButton.BackgroundColor = Color.FromArgb("#FF6B35");
                    enButton.TextColor = Colors.White;
                    viButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                    viButton.TextColor = Color.FromArgb("#1F1F1F");
                }
            }
        }
    }
}