using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class RestaurantDetailPage : ContentPage
    {
        private readonly Restaurant _restaurant;
        private readonly AudioPlaybackService _audioService;
        private readonly DatabaseService _databaseService;
        private readonly LocalizationService _localizationService;

        private string _selectedAudioLanguage = "vi";

        public RestaurantDetailPage(Restaurant restaurant, AudioPlaybackService? audioService = null)
        {
            InitializeComponent();
            _restaurant = restaurant;
            _audioService = audioService ?? new AudioPlaybackService();
            _databaseService = ResolveService<DatabaseService>() ?? new DatabaseService();
            _localizationService = LocalizationService.Instance;

            InitializeAudioLanguage();
            LoadRestaurantData();
            _ = LoadMenuItemsAsync();

            _localizationService.LanguageChanged += OnLanguageChangedEvent;
        }

        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        private void InitializeAudioLanguage()
        {
            var currentLanguage = _localizationService.CurrentLanguage;
            _selectedAudioLanguage = currentLanguage is "en" or "zh" or "ja" or "ru" or "fr"
                ? currentLanguage
                : "vi";

            if (!HasTextForLanguage(_selectedAudioLanguage))
            {
                _selectedAudioLanguage = "vi";
            }

            ApplyLanguageAvailability();
            UpdateAudioOptionUI();
        }

        private bool HasTextForLanguage(string language)
        {
            return language switch
            {
                "en" => !string.IsNullOrWhiteSpace(_restaurant.TextEn),
                "zh" => !string.IsNullOrWhiteSpace(_restaurant.TextZh),
                "ja" => !string.IsNullOrWhiteSpace(_restaurant.TextJa),
                "ru" => !string.IsNullOrWhiteSpace(_restaurant.TextRu),
                "fr" => !string.IsNullOrWhiteSpace(_restaurant.TextFr),
                _ => !string.IsNullOrWhiteSpace(_restaurant.TextVi) || !string.IsNullOrWhiteSpace(_restaurant.History)
            };
        }

        private void ApplyLanguageAvailability()
        {
            ViOptionButton.IsVisible = HasTextForLanguage("vi");
            EnOptionButton.IsVisible = HasTextForLanguage("en");
            ZhOptionButton.IsVisible = HasTextForLanguage("zh");
            JaOptionButton.IsVisible = HasTextForLanguage("ja");
            RuOptionButton.IsVisible = HasTextForLanguage("ru");
            FrOptionButton.IsVisible = HasTextForLanguage("fr");

            if (!HasTextForLanguage(_selectedAudioLanguage))
            {
                _selectedAudioLanguage = "vi";
            }
        }

        private void LoadRestaurantData()
        {
            var language = _localizationService.CurrentLanguage;
            RestaurantNameLabel.Text = _restaurant.Name;
            YearLabel.Text = $"{_localizationService.GetString("YearEstablished", language)}: {_restaurant.YearEstablished}";
            RatingLabel.Text = $"⭐ {_restaurant.Rating:F1}";
            HistoryLabel.Text = _restaurant.History;
            IntroLabel.Text = _restaurant.GetTextByLanguage(language);
            AddressTitleLabel.Text = _localizationService.GetString("Address", language);
            AddressLabel.Text = string.IsNullOrWhiteSpace(_restaurant.Address)
                ? "-"
                : _restaurant.Address;
            NarrationLanguageLabel.Text = _localizationService.GetString("NarrationLanguage", language);
            SignatureDishLabel.Text = _restaurant.Name;
            SignatureDishStoryLabel.Text = _restaurant.History;
        }

        private async Task LoadMenuItemsAsync()
        {
            if (!int.TryParse(_restaurant.Id, out var poiId))
            {
                MenuCollection.ItemsSource = Array.Empty<PoiMenuItem>();
                return;
            }

            try
            {
                await _databaseService.InitAsync();
                var menuItems = await _databaseService.GetMenuItemsAsync(poiId);
                MenuCollection.ItemsSource = menuItems;

                var signature = menuItems.FirstOrDefault(i => i.IsSignature) ?? menuItems.FirstOrDefault();
                if (signature is not null)
                {
                    SignatureDishLabel.Text = signature.Name;
                    if (!string.IsNullOrWhiteSpace(signature.Description))
                    {
                        SignatureDishStoryLabel.Text = signature.Description;
                    }
                }
            }
            catch
            {
                MenuCollection.ItemsSource = Array.Empty<PoiMenuItem>();
            }
        }

        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            LoadRestaurantData();
            ApplyLanguageAvailability();
            UpdateAudioOptionUI();
        }

        private void OnAudioOptionClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: string language })
            {
                return;
            }

            _selectedAudioLanguage = language;
            UpdateAudioOptionUI();
        }

        private void SetLanguageButtonState(Button button, bool isSelected)
        {
            var activeBg = Color.FromArgb("#FF6B35");
            var inactiveBg = Color.FromArgb("#E0E0E0");
            var activeText = Colors.White;
            var inactiveText = Color.FromArgb("#1F1F1F");

            button.BackgroundColor = isSelected ? activeBg : inactiveBg;
            button.TextColor = isSelected ? activeText : inactiveText;
        }

        private void UpdateAudioOptionUI()
        {
            SetLanguageButtonState(ViOptionButton, _selectedAudioLanguage == "vi");
            SetLanguageButtonState(EnOptionButton, _selectedAudioLanguage == "en");
            SetLanguageButtonState(ZhOptionButton, _selectedAudioLanguage == "zh");
            SetLanguageButtonState(JaOptionButton, _selectedAudioLanguage == "ja");
            SetLanguageButtonState(RuOptionButton, _selectedAudioLanguage == "ru");
            SetLanguageButtonState(FrOptionButton, _selectedAudioLanguage == "fr");
        }

        private async void OnPlaySignatureDish(object sender, EventArgs e)
        {
            var ttsText = _restaurant.GetTextByLanguage(_selectedAudioLanguage);

            await _audioService.PlayTextAsync(
                ttsText,
                _selectedAudioLanguage,
                _restaurant.Name,
                _restaurant.Id);
        }

        private void OnPlayClicked(object sender, EventArgs e)
        {
            _audioService.Resume();
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            _audioService.Pause();
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            await _audioService.StopAsync();
        }
    }
}