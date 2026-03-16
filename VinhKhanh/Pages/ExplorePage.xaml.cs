using System.Globalization;
using System.Text.Json;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class ExplorePage : ContentPage
{
	private const string TrackAsiaApiKey = "3a82d12156488a8391773657171aacb765";
	private const string TrackAsiaCssUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.css";
	private const string TrackAsiaJsUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.js";
	private const string MapLibreCssUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css";
	private const string MapLibreJsUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js";

	private AudioPlaybackService _audioService;
	private readonly List<Restaurant> _restaurants = new();

	public ExplorePage()
	{
		InitializeComponent();
		_audioService = new AudioPlaybackService();
		RenderTrackAsiaMap();
	}

	public void SetCategories(List<Category> categories)
	{
		FilterChipsCollection.ItemsSource = categories;
	}

	public void SetRestaurants(List<Restaurant> restaurants)
	{
		RestaurantsCollection.ItemsSource = restaurants;

		_restaurants.Clear();
		if (restaurants != null && restaurants.Count > 0)
		{
			_restaurants.AddRange(restaurants);
		}

		RenderTrackAsiaMap();
	}

	private void RenderTrackAsiaMap()
	{
		var mapWebView = this.FindByName<WebView>("TrackAsiaMapWebView");
		if (mapWebView is null)
		{
			return;
		}

		mapWebView.Source = new HtmlWebViewSource
		{
			Html = BuildMapHtml(_restaurants)
		};
	}

	private static string EscapeHtml(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		return value
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&#39;");
	}

	private string BuildMapHtml(IReadOnlyList<Restaurant> restaurants)
	{
		var centerLat = restaurants.Count > 0 ? restaurants[0].Latitude : 10.7769;
		var centerLng = restaurants.Count > 0 ? restaurants[0].Longitude : 106.6966;

		var restaurantPayload = restaurants.Select(r => new
		{
			id = r.Id,
			name = EscapeHtml(r.Name),
			highlights = EscapeHtml(r.Highlights),
			rating = r.Rating,
			latitude = r.Latitude,
			longitude = r.Longitude
		}).ToList();

		var restaurantsJson = JsonSerializer.Serialize(restaurantPayload);

		var html = @"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />
  <link rel='stylesheet' href='__TRACKASIA_CSS__' />
  <link rel='stylesheet' href='__MAPLIBRE_CSS__' />
  <style>
    html, body, #map { margin: 0; padding: 0; width: 100%; height: 100%; background: #ffffff; }
    .popup-title { font-weight: 700; margin-bottom: 4px; color: #1F1F1F; }
    .popup-sub { font-size: 12px; color: #666666; margin: 0; }
  </style>
</head>
<body>
  <div id='map'></div>
  <script src='__TRACKASIA_JS__'></script>
  <script src='__MAPLIBRE_JS__'></script>
  <script>
    const apiKey = '__API_KEY__';
    const restaurants = __RESTAURANTS_JSON__;

    function getMapSdk() {
      if (window.trackasia && window.trackasia.Map) return window.trackasia;
      if (window.maplibregl && window.maplibregl.Map) return window.maplibregl;
      return null;
    }

    function initMap() {
      const sdk = getMapSdk();
      if (!sdk) {
        document.body.innerHTML = '<div style=""padding:12px;color:#B00020;font-family:sans-serif;"">Không tải được Map SDK.</div>';
        return;
      }

      const map = new sdk.Map({
        container: 'map',
        style: 'https://maps.track-asia.com/styles/v1/streets.json?key=' + apiKey,
        center: [__CENTER_LNG__, __CENTER_LAT__],
        zoom: 14
      });

      if (sdk.NavigationControl) {
        map.addControl(new sdk.NavigationControl(), 'top-right');
      }

      restaurants.forEach(r => {
        const popupHtml =
          '<div class=""popup-title"">' + (r.name ?? '') + '</div>' +
          '<p class=""popup-sub"">⭐ ' + Number(r.rating).toFixed(1) + '</p>' +
          '<p class=""popup-sub"">' + (r.highlights ?? '') + '</p>';

        const popup = new sdk.Popup({ offset: 20 }).setHTML(popupHtml);
        new sdk.Marker({ color: '#FF6B35' })
          .setLngLat([r.longitude, r.latitude])
          .setPopup(popup)
          .addTo(map);
      });
    }

    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initMap);
    } else {
      initMap();
    }
  </script>
</body>
</html>";

		return html
			.Replace("__TRACKASIA_CSS__", TrackAsiaCssUrl)
			.Replace("__MAPLIBRE_CSS__", MapLibreCssUrl)
			.Replace("__TRACKASIA_JS__", TrackAsiaJsUrl)
			.Replace("__MAPLIBRE_JS__", MapLibreJsUrl)
			.Replace("__API_KEY__", TrackAsiaApiKey)
			.Replace("__RESTAURANTS_JSON__", restaurantsJson)
			.Replace("__CENTER_LAT__", centerLat.ToString(CultureInfo.InvariantCulture))
			.Replace("__CENTER_LNG__", centerLng.ToString(CultureInfo.InvariantCulture));
	}

	private async void OnPlayAudioClicked(object sender, EventArgs e)
	{
		var button = sender as Button;
		var restaurant = button?.CommandParameter as Restaurant;

		if (restaurant != null)
		{
			var localizationService = LocalizationService.Instance;
			await _audioService.PlayAudioAsync(new AudioContent
			{
				RestaurantId = restaurant.Id,
				Language = localizationService.CurrentLanguage,
				ContentType = "signature_dish",
				Title = restaurant.Name,
				AudioUrl = $"https://your-server.com/audio/{restaurant.Id}_signature_{localizationService.CurrentLanguage}.mp3"
			});
		}
	}

	private async void OnViewDetailsClicked(object sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: Restaurant restaurant })
		{
			return;
		}

		await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
	}
}