using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class RestaurantDetailPage : ContentPage
    {
        private readonly Restaurant _restaurant;
        private readonly AudioPlaybackService _audioService;

        private string _selectedAudioLanguage = "vi";

        public RestaurantDetailPage(Restaurant restaurant, AudioPlaybackService? audioService = null)
        {
            InitializeComponent();
            _restaurant = restaurant;
            _audioService = audioService ?? new AudioPlaybackService();

            InitializeAudioLanguage();
            LoadRestaurantData();
        }

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
            SignatureDishLabel.Text = _restaurant.Name;
            SignatureDishStoryLabel.Text = _restaurant.History;
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
            await _audioService.PlayAudioAsync(new AudioContent
            {
                RestaurantId = _restaurant.Id,
                Language = _selectedAudioLanguage,
                ContentType = "signature_dish",
                Title = _restaurant.Name,
                AudioUrl = $"https://your-server.com/audio/{_restaurant.Id}_signature_{_selectedAudioLanguage}.mp3"
            });
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