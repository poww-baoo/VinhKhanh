using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class RestaurantDetailPage : ContentPage
    {
        private Restaurant _restaurant;
        private AudioPlaybackService _audioService;

        public RestaurantDetailPage(Restaurant restaurant, AudioPlaybackService audioService = null)
        {
            InitializeComponent();
            _restaurant = restaurant;
            _audioService = audioService ?? new AudioPlaybackService();
            LoadRestaurantData();
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

        private async void OnPlaySignatureDish(object sender, EventArgs e)
        {
            await _audioService.PlayAudioAsync(new AudioContent
            {
                RestaurantId = _restaurant.Id,
                Language = "vi",
                ContentType = "signature_dish",
                Title = _restaurant.Name,
                AudioUrl = $"https://your-server.com/audio/{_restaurant.Id}_signature_vi.mp3"
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