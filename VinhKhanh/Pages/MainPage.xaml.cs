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
        private List<Category> _categories = new();
        private bool _isTracking = false;
        private string _currentTab = "explore";
            
        public MainPage()
        {
            InitializeComponent();
            InitializeServices();
            LoadCategories();
            LoadRestaurants();
            RefreshTrackAsiaMap();
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
            RefreshTrackAsiaMap();
        }

        private void RefreshTrackAsiaMap()
        {
            if (MapWebView == null || _restaurants.Count == 0)
                return;

            MapWebView.Source = new HtmlWebViewSource
            {
                Html = BuildTrackAsiaMapHtml(_restaurants),
                BaseUrl = "https://maps.track-asia.com/"
            };
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

        private string BuildTrackAsiaMapHtml(List<Restaurant> restaurants)
        {
            var center = restaurants[0];
            var centerLng = center.Longitude.ToString(CultureInfo.InvariantCulture);
            var centerLat = center.Latitude.ToString(CultureInfo.InvariantCulture);

            var markers = new StringBuilder();
            foreach (var r in restaurants)
            {
                var lng = r.Longitude.ToString(CultureInfo.InvariantCulture);
                var lat = r.Latitude.ToString(CultureInfo.InvariantCulture);
                var name = EscapeJs(r.Name);
                var rating = r.Rating.ToString("F1", CultureInfo.InvariantCulture);

                markers.AppendLine($@"
new sdk.Marker({{ color: '#FF6B35' }})
    .setLngLat([{lng}, {lat}])
    .setPopup(new sdk.Popup({{ offset: 12 }}).setHTML('<b>{name}</b><br/>⭐ {rating}'))
    .addTo(map);");
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0' />

<link href='{TrackAsiaCssUrl}' rel='stylesheet' />
<link href='{MapLibreCssUrl}' rel='stylesheet' />

<style>
  html, body, #map {{
    margin: 0; width: 100%; height: 100%; overflow: hidden;
  }}
  .err {{
    padding: 12px; color: #c00; font-family: sans-serif; font-size: 13px;
  }}
</style>
</head>
<body>
<div id='map'></div>

<script src='{TrackAsiaJsUrl}'></script>
<script src='{MapLibreJsUrl}'></script>

<script>
(function() {{
  const sdk = window.trackasia || window.mapboxgl || window.maplibregl;

  if (!sdk) {{
    document.getElementById('map').innerHTML =
      '<div class=""err"">Không tải được SDK map. Kiểm tra lại URL JS/CSS trong docs TrackAsia.</div>';
    return;
  }}

  // Một số SDK cần accessToken, một số thì không
  if ('accessToken' in sdk) {{
    sdk.accessToken = '{TrackAsiaApiKey}';
  }}

  const styleUrl = 'https://maps.track-asia.com/styles/v1/streets.json?key={TrackAsiaApiKey}';

  const map = new sdk.Map({{
    container: 'map',
    style: styleUrl,
    center: [{centerLng}, {centerLat}],
    zoom: 15
  }});

  if (sdk.NavigationControl) {{
    map.addControl(new sdk.NavigationControl(), 'top-right');
  }}

  {markers}
}})();
</script>
</body>
</html>";
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