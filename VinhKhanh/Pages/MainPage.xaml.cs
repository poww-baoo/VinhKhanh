using System.Globalization;
using System.Text;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class MainPage : ContentPage
    {
        private const string TrackAsiaApiKey = "3a82d12156488a8391773657171aacb765";

        // Nếu SDK URL của TrackAsia docs khác, chỉ cần đổi 2 URL này
        private const string TrackAsiaCssUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.css";
        private const string TrackAsiaJsUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.js";

        // Fallback chắc chắn có sẵn
        private const string MapLibreCssUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css";
        private const string MapLibreJsUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js";

        private LocationService _locationService;
        private AudioPlaybackService _audioService;
        private LocalizationService _localizationService;
        private List<Restaurant> _restaurants = new();
        private string _currentTab = "explore";
        private ExplorePage _explorePage;
        private SavedPage _savedPage;
        private TrackingPage _trackingPage;
        private SettingsPage _settingsPage;

        public MainPage()
        {
            InitializeComponent();
            InitializeServices();
            InitializePages();
            LoadCategories();
            LoadRestaurants();
            ShowTabContent("explore");
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

        private void InitializePages()
        {
            _explorePage = new ExplorePage();
            _savedPage = new SavedPage();
            _trackingPage = new TrackingPage();
            _settingsPage = new SettingsPage();
        }

        private void LoadCategories()
        {
            var categories = new List<Category>
            {
                new Category { Id = "1", Name = "Cơm Tấm" },
                new Category { Id = "2", Name = "Bánh Mì" },
                new Category { Id = "3", Name = "Phở" },
                new Category { Id = "4", Name = "Bún" },
                new Category { Id = "5", Name = "Súp" }
            };

            // Pass categories to ExplorePage
            _explorePage.SetCategories(categories);
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

            // Pass restaurants to ExplorePage
            _explorePage.SetRestaurants(_restaurants);
            _trackingPage.SetRestaurants(_restaurants);
        }

        private static string EscapeJs(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
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
            ContentFrame.Children.Clear();

            ContentPage pageToShow = tab switch
            {
                "explore" => _explorePage,
                "saved" => _savedPage,
                "tracking" => _trackingPage,
                "settings" => _settingsPage,
                _ => _explorePage
            };

            // Extract the main content from the page
            var view = pageToShow.Content;
            if (view != null)
            {
                var wrapper = new ContentView { Content = view };
                ContentFrame.Children.Add(wrapper);
            }
        }

        private void OnLocationUpdated(object? sender, Location location)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _trackingPage.UpdateLocation(location);
            });
        }

        private async void OnEnteredGeofence(object? sender, Restaurant restaurant)
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
                _trackingPage.UpdateStatus($"Đang phát thuyết minh: {restaurant.Name}");
            });
        }

        private void OnPlaybackCompleted(object? sender, AudioContent e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _trackingPage.UpdateStatus("Phát thuyết minh xong");
            });
        }

        private void OnLocalizationLanguageChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadRestaurants();
            });
        }
    }
}