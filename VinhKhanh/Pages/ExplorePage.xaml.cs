using System.Globalization;
using System.Text;
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

    private readonly AudioPlaybackService _audioService;

    private readonly List<Restaurant> _allRestaurants = new();
    private readonly List<Restaurant> _filteredRestaurants = new();
    private readonly List<Category> _categories = new();

    private int? _selectedCategoryId;
    private string _searchKeyword = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;

    public ExplorePage()
    {
        InitializeComponent();
        _audioService = new AudioPlaybackService();
        RenderTrackAsiaMap();
    }

    public void SetCategories(List<Category> categories)
    {
        _categories.Clear();
        if (categories is { Count: > 0 })
        {
            _categories.AddRange(categories.OrderBy(c => c.SortOrder));
        }

        var allCategory = _categories.FirstOrDefault(c => c.Name.Equals("Tất cả", StringComparison.OrdinalIgnoreCase))
                          ?? _categories.FirstOrDefault(c => c.SortOrder == 0)
                          ?? _categories.FirstOrDefault();

        if (allCategory is not null)
        {
            _selectedCategoryId = allCategory.Id;
            UpdateCategorySelectionState();
        }

        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    public void SetRestaurants(List<Restaurant> restaurants)
    {
        _allRestaurants.Clear();
        if (restaurants is { Count: > 0 })
        {
            _allRestaurants.AddRange(restaurants);
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<Restaurant> query = _allRestaurants;

        if (_selectedCategoryId.HasValue)
        {
            var selectedCategory = _categories.FirstOrDefault(c => c.Id == _selectedCategoryId.Value);
            var isAll = selectedCategory?.Name.Equals("Tất cả", StringComparison.OrdinalIgnoreCase) == true;
            if (!isAll)
            {
                query = query.Where(r => r.CategoryId == _selectedCategoryId.Value);
            }
        }

        var keyword = _searchKeyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = NormalizeForSearch(keyword);

            query = query.Where(r =>
            {
                var searchBlob = NormalizeForSearch($"{r.Name} {r.History} {r.TextVi} {r.Highlights} {r.CategoryName}");
                return searchBlob.Contains(normalizedKeyword, StringComparison.Ordinal);
            });
        }

        _filteredRestaurants.Clear();
        _filteredRestaurants.AddRange(query.OrderBy(r => r.Priority));

        RestaurantsCollection.ItemsSource = null;
        RestaurantsCollection.ItemsSource = _filteredRestaurants;

        ResultCountLabel.Text = $"{_filteredRestaurants.Count} kết quả";
        RenderTrackAsiaMap();
    }

    private static string NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }

    private void RenderTrackAsiaMap()
    {
        var html = BuildMapHtml(_filteredRestaurants);
        SetMapHtml("TrackAsiaMapWebView", html);
        SetMapHtml("TrackAsiaMapFullScreenWebView", html);
    }

    private void SetMapHtml(string webViewName, string html)
    {
        var mapWebView = this.FindByName<WebView>(webViewName);
        if (mapWebView is null)
        {
            return;
        }

        mapWebView.Source = new HtmlWebViewSource
        {
            Html = html
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
    .popup-link { margin-top: 6px; display: inline-block; color: #FF6B35; text-decoration: none; font-weight: 700; }
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
        const detailUrl = 'app://poi/' + encodeURIComponent(r.id ?? '');
        const popupHtml =
          '<div class=""popup-title"">' + (r.name ?? '') + '</div>' +
          '<p class=""popup-sub"">⭐ ' + Number(r.rating).toFixed(1) + '</p>' +
          '<p class=""popup-sub"">' + (r.highlights ?? '') + '</p>' +
          '<a class=""popup-link"" href=""' + detailUrl + '"">Xem chi tiết</a>';

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
        if (sender is not Button { CommandParameter: Restaurant restaurant })
        {
            return;
        }

        var language = LocalizationService.Instance.CurrentLanguage;
        var ttsText = string.IsNullOrWhiteSpace(restaurant.TextVi)
            ? restaurant.History
            : restaurant.TextVi;

        await _audioService.PlayTextAsync(
            ttsText,
            language,
            restaurant.Name,
            restaurant.Id);
    }

    private async void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Restaurant restaurant })
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
    }

    private async void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Url))
        {
            return;
        }

        if (!e.Url.StartsWith("app://poi/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        var encodedId = e.Url.Replace("app://poi/", string.Empty, StringComparison.OrdinalIgnoreCase);
        var poiId = Uri.UnescapeDataString(encodedId);

        var restaurant = _allRestaurants.FirstOrDefault(r => r.Id == poiId);
        if (restaurant is null)
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
    }

    private void UpdateCategorySelectionState()
    {
        foreach (var category in _categories)
        {
            category.IsSelected = _selectedCategoryId.HasValue && category.Id == _selectedCategoryId.Value;
        }
    }

    private void RefreshCategoryItemsSource()
    {
        FilterChipsCollection.ItemsSource = null;
        FilterChipsCollection.ItemsSource = _categories;
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedCategory = e.CurrentSelection?.FirstOrDefault() as Category;
        _selectedCategoryId = selectedCategory?.Id;
        UpdateCategorySelectionState();
        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    private void OnCategoryChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Category category)
        {
            return;
        }

        _selectedCategoryId = category.Id;
        UpdateCategorySelectionState();
        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchKeyword = e.NewTextValue ?? string.Empty;

        _searchDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(ApplyFilters);
            }
            catch (TaskCanceledException)
            {
                // bỏ qua khi người dùng đang tiếp tục gõ
            }
        });
    }

    private void OnExpandMapClicked(object sender, EventArgs e)
    {
        var overlay = this.FindByName<Grid>("FullScreenMapOverlay");
        if (overlay is null)
        {
            return;
        }

        overlay.IsVisible = true;
    }

    private void OnCloseFullScreenMapClicked(object sender, EventArgs e)
    {
        var overlay = this.FindByName<Grid>("FullScreenMapOverlay");
        if (overlay is null)
        {
            return;
        }

        overlay.IsVisible = false;
    }
}