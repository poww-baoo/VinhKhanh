using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class MainPage : ContentPage
    {
        private LocationService _locationService;
        private AudioPlaybackService _audioService;
        private LocalizationService _localizationService;
        private List<Restaurant> _restaurants = new();
        private List<Category> _categories = new();
        private bool _isTracking = false;
        private string _currentTab = "explore";

        public MainPage()
        {
            InitializeComponent();
            InitializeServices();
            LoadCategories();
            LoadRestaurants();
        }

        private void InitializeServices()
        {
            _locationService = new LocationService();
            _audioService = new AudioPlaybackService();
            _localizationService = LocalizationService.Instance;

            _locationService.LocationUpdated += OnLocationUpdated;
            _locationService.EnteredGeofence += OnEnteredGeofence;
            _audioService.PlaybackCompleted += OnPlaybackCompleted;
            _localizationService.LanguageChanged += OnLocalizationLanguageChanged;
        }

        private void LoadCategories()
        {
            _categories = new List<Category>
            {
                new Category { Id = "1", Name = "Cơm Tấm" },
                new Category { Id = "2", Name = "Bánh Mì" },
                new Category { Id = "3", Name = "Phở" },
                new Category { Id = "4", Name = "Bún" },
                new Category { Id = "5", Name = "Súp" }
            };

            FilterChipsCollection.ItemsSource = _categories;
        }

        private void LoadRestaurants()
        {
            _restaurants = new List<Restaurant>
            {
                new Restaurant
                {
                    Id = "1",
                    Name = "Quán Cơm Tấm Truyền Thống",
                    YearEstablished = 1985,
                    History = "Quán cơm tấm được thành lập từ năm 1985...",
                    Highlights = "Cơm tấm, thịt nướng, trứng chưng",
                    Rating = 4.8,
                    Latitude = 10.7769,
                    Longitude = 106.6966,
                    Priority = 1
                },
                new Restaurant
                {
                    Id = "2",
                    Name = "Bánh Mì Sài Gòn",
                    YearEstablished = 1995,
                    History = "Quán bánh mì nổi tiếng với công thức độc đáo...",
                    Highlights = "Bánh mì nóng, pâté, chả lua",
                    Rating = 4.7,
                    Latitude = 10.7771,
                    Longitude = 106.6968,
                    Priority = 2
                }
            };

            RestaurantsCollection.ItemsSource = _restaurants;
        }

        private void OnTabTapped(object sender, TappedEventArgs e)
        {
            var tab = e.Parameter as string;

            if (tab == null) return;

            _currentTab = tab;
            UpdateTabUI();
            ShowTabContent(tab);
        }

        private void UpdateTabUI()
        {
            ExploreTab.Opacity = _currentTab == "explore" ? 1.0 : 0.5;
            SavedTab.Opacity = _currentTab == "saved" ? 1.0 : 0.5;
            TrackingTab.Opacity = _currentTab == "tracking" ? 1.0 : 0.5;
            SettingsTab.Opacity = _currentTab == "settings" ? 1.0 : 0.5;

            var exploreLabelTextColor = _currentTab == "explore" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#1F1F1F");
            var savedLabelTextColor = _currentTab == "saved" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            var trackingLabelTextColor = _currentTab == "tracking" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            var settingsLabelTextColor = _currentTab == "settings" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");

            ((Label)ExploreTab.Children[1]).TextColor = exploreLabelTextColor;
            ((Label)SavedTab.Children[1]).TextColor = savedLabelTextColor;
            ((Label)TrackingTab.Children[1]).TextColor = trackingLabelTextColor;
            ((Label)SettingsTab.Children[1]).TextColor = settingsLabelTextColor;
        }

        private void ShowTabContent(string tab)
        {
            ExplorePage.IsVisible = tab == "explore";
            SavedPage.IsVisible = tab == "saved";
            TrackingPage.IsVisible = tab == "tracking";
            SettingsPage.IsVisible = tab == "settings";
        }

        private async void OnTrackingToggled(object sender, EventArgs e)
        {
            _isTracking = !_isTracking;

            if (_isTracking)
            {
                TrackingButton.Text = "⏸️ Dừng Theo dõi";
                TrackingButton.BackgroundColor = Colors.Red;
                StatusLabel.Text = "Đang theo dõi vị trí...";
                TrackingStatusLabel.Text = "Trạng thái: Đang theo dõi";
                await _locationService.StartTrackingAsync(_restaurants);
            }
            else
            {
                TrackingButton.Text = "▶️ BẮT ĐẦU THEO DÕI";
                TrackingButton.BackgroundColor = Color.FromArgb("#FF6B35");
                StatusLabel.Text = "Nhấn để bắt đầu khám phá";
                TrackingStatusLabel.Text = "Trạng thái: Chưa bắt đầu";
                _locationService.StopTracking();
            }
        }

        private void OnLocationUpdated(object sender, Location location)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LocationLabel.Text = $"Lat: {location.Latitude:F4}, Lng: {location.Longitude:F4}";
            });
        }

        private async void OnEnteredGeofence(object sender, Restaurant restaurant)
        {
            await _audioService.PlayAudioAsync(new AudioContent
            {
                RestaurantId = restaurant.Id,
                Language = _localizationService.CurrentLanguage,
                ContentType = "signature_dish",
                Title = restaurant.Name,
                AudioUrl = $"https://your-server.com/audio/{restaurant.Id}_signature_{_localizationService.CurrentLanguage}.mp3"
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = $"Đang phát thuyết minh: {restaurant.Name}";
            });
        }

        private async void OnPlayAudioClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var restaurant = button?.CommandParameter as Restaurant;

            if (restaurant != null)
            {
                await _audioService.PlayAudioAsync(new AudioContent
                {
                    RestaurantId = restaurant.Id,
                    Language = _localizationService.CurrentLanguage,
                    ContentType = "signature_dish",
                    Title = restaurant.Name,
                    AudioUrl = $"https://your-server.com/audio/{restaurant.Id}_signature_{_localizationService.CurrentLanguage}.mp3"
                });
            }
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var restaurant = button?.CommandParameter as Restaurant;

            if (restaurant != null)
            {
                await Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.CommandParameter is string language)
            {
                _localizationService.CurrentLanguage = language;
            }
        }

        private void OnLocalizationLanguageChanged(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadRestaurants();
            });
        }

        private void OnPlaybackCompleted(object sender, AudioContent e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "Phát thuyết minh xong";
            });
        }
    }
}