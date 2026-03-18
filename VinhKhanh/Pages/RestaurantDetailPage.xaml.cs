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

        private string _selectedAudioLanguage = "vi";

        public RestaurantDetailPage(Restaurant restaurant, AudioPlaybackService? audioService = null)
        {
            InitializeComponent();
            _restaurant = restaurant;
            _audioService = audioService ?? new AudioPlaybackService();
            _databaseService = ResolveService<DatabaseService>() ?? new DatabaseService();

            InitializeAudioLanguage();
            LoadRestaurantData();
            _ = LoadMenuItemsAsync();
        }

        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        private void InitializeAudioLanguage()
        {
            var currentLanguage = LocalizationService.Instance.CurrentLanguage;
            _selectedAudioLanguage = currentLanguage is "en" ? "en" : "vi";
            UpdateAudioOptionUI();
        }

        private void LoadRestaurantData()
        {
            RestaurantNameLabel.Text = _restaurant.Name;
            YearLabel.Text = $"Thành lập: {_restaurant.YearEstablished}";
            RatingLabel.Text = $"⭐ {_restaurant.Rating:F1}";
            HistoryLabel.Text = _restaurant.History;
            IntroLabel.Text = string.IsNullOrWhiteSpace(_restaurant.TextVi)
                ? _restaurant.History
                : _restaurant.TextVi;
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

        private void OnAudioOptionClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: string language })
            {
                return;
            }

            _selectedAudioLanguage = language;
            LocalizationService.Instance.CurrentLanguage = language;
            UpdateAudioOptionUI();
        }

        private void UpdateAudioOptionUI()
        {
            var activeBg = Color.FromArgb("#FF6B35");
            var inactiveBg = Color.FromArgb("#E0E0E0");
            var activeText = Colors.White;
            var inactiveText = Color.FromArgb("#1F1F1F");

            var isVi = _selectedAudioLanguage == "vi";

            ViOptionButton.BackgroundColor = isVi ? activeBg : inactiveBg;
            ViOptionButton.TextColor = isVi ? activeText : inactiveText;

            EnOptionButton.BackgroundColor = isVi ? inactiveBg : activeBg;
            EnOptionButton.TextColor = isVi ? inactiveText : activeText;
        }

        private async void OnPlaySignatureDish(object sender, EventArgs e)
        {
            var ttsText = string.IsNullOrWhiteSpace(_restaurant.TextVi)
                ? _restaurant.History
                : _restaurant.TextVi;

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