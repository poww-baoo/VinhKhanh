using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class TrackingPage : ContentPage
{
	private Location? _currentLocation;
	private List<Restaurant> _allRestaurants = new();
	private List<Restaurant> _nearbyRestaurants = new();
	private bool _isTrackingEnabled = false;
	private LocationService? _locationService;
	private readonly LocalizationService _localizationService;

	private const double NearbyRadiusMeters = 3000000;
	private const int MaxNearbyRestaurants = 10;

	public TrackingPage()
	{
		InitializeComponent();
		_localizationService = LocalizationService.Instance;
		_localizationService.LanguageChanged += OnLanguageChangedEvent;
		RestaurantsCollection.ItemsSource = _nearbyRestaurants;
		UpdateUI();
	}

	private void OnLanguageChangedEvent(object? sender, EventArgs e)
	{
		UpdateUI();
	}

	private void UpdateUI()
	{
		var language = _localizationService.CurrentLanguage;

		Title = _localizationService.GetString("Tracking", language);

		CurrentLocationTitleLabel.Text = _localizationService.GetString("CurrentLocationTracking", language);
		StatusTitleLabel.Text = _localizationService.GetString("Status", language);
		NearbyRestaurantsTitleLabel.Text = _localizationService.GetString("NearbyRestaurants", language);

		if (_currentLocation is not null)
		{
			LocationLabel.Text =
				$"{_localizationService.GetString("Latitude", language)}: {_currentLocation.Latitude:F4}, " +
				$"{_localizationService.GetString("Longitude", language)}: {_currentLocation.Longitude:F4}";
		}
		else
		{
			LocationLabel.Text = _localizationService.GetString("WaitingGPS", language);
		}

		UpdateTrackingUI();
	}

	public void SetRestaurants(List<Restaurant> restaurants)
	{
		_allRestaurants = restaurants ?? new List<Restaurant>();
		RefreshNearbyRestaurants();
	}

	public void SetLocationService(LocationService locationService)
	{
		_locationService = locationService;
	}

	/// <summary>
	/// Bắt đầu tracking tự động khi app mở (gọi từ MainPage)
	/// </summary>
	public async void EnableTrackingUI()
	{
		_isTrackingEnabled = true;
		UpdateTrackingUI();
		// Update UI trạng thái nhưng không cần gọi StartTracking lại vì LocationService đã được khởi động
	}

	private void OnTrackingToggleClicked(object sender, EventArgs e)
	{
		_isTrackingEnabled = !_isTrackingEnabled;
		UpdateTrackingUI();

		if (_isTrackingEnabled)
		{
			StartTracking();
		}
		else
		{
			StopTracking();
		}
	}

	private void UpdateTrackingUI()
	{
		var language = _localizationService.CurrentLanguage;

		if (_isTrackingEnabled)
		{
			TrackingToggleButton.Text = _localizationService.GetString("StopTracking", language);
			TrackingToggleButton.BackgroundColor = Color.FromArgb("#E74C3C");
			StatusLabel.Text = _localizationService.GetString("Tracking_Enabled", language);
		}
		else
		{
			TrackingToggleButton.Text = _localizationService.GetString("StartTracking", language);
			TrackingToggleButton.BackgroundColor = Color.FromArgb("#FF6B35");
			StatusLabel.Text = _localizationService.GetString("Ready", language);
		}
	}

	private async void StartTracking()
	{
		try
		{
			if (_locationService != null)
			{
				await _locationService.StartTrackingAsync(_allRestaurants);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					var language = _localizationService.CurrentLanguage;
					DisplayAlert(
						_localizationService.GetString("TrackingSuccess", language),
						_localizationService.GetString("TrackingStarted", language),
						_localizationService.GetString("OK", language));
				});
			}
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var language = _localizationService.CurrentLanguage;
				DisplayAlert(
					_localizationService.GetString("TrackingError", language),
					$"{_localizationService.GetString("StartTrackingError", language)}: {ex.Message}",
					_localizationService.GetString("OK", language));
				_isTrackingEnabled = false;
				UpdateTrackingUI();
			});
		}
	}

	private void StopTracking()
	{
		try
		{
			if (_locationService != null)
			{
				_locationService.StopTracking();

				_nearbyRestaurants.Clear();
				RestaurantsCollection.ItemsSource = null;
				RestaurantsCollection.ItemsSource = _nearbyRestaurants;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					var language = _localizationService.CurrentLanguage;
					DisplayAlert(
						_localizationService.GetString("TrackingSuccess", language),
						_localizationService.GetString("TrackingStopped", language),
						_localizationService.GetString("OK", language));
				});
			}
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var language = _localizationService.CurrentLanguage;
				DisplayAlert(
					_localizationService.GetString("TrackingError", language),
					$"{_localizationService.GetString("StopTrackingError", language)}: {ex.Message}",
					_localizationService.GetString("OK", language));
			});
		}
	}

	public void UpdateLocation(Location location)
	{
		try
		{
			var language = _localizationService.CurrentLanguage;
			_currentLocation = location;
			LocationLabel.Text = $"{_localizationService.GetString("Latitude", language)}: {location.Latitude:F4}, {_localizationService.GetString("Longitude", language)}: {location.Longitude:F4}";

			RefreshNearbyRestaurants();
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var language = _localizationService.CurrentLanguage;
				DisplayAlert(
					_localizationService.GetString("Error", language),
					$"{_localizationService.GetString("LocationError", language)}: {ex.Message}",
					_localizationService.GetString("OK", language));
			});
		}
	}

	public void UpdateStatus(string status)
	{
		try
		{
			if (!_isTrackingEnabled) return;
			StatusLabel.Text = status;
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var language = _localizationService.CurrentLanguage;
				DisplayAlert(
					_localizationService.GetString("Error", language),
					$"{_localizationService.GetString("StatusError", language)}: {ex.Message}",
					_localizationService.GetString("OK", language));
			});
		}
	}

    private void RefreshNearbyRestaurants()
    {
        if (!_isTrackingEnabled || _currentLocation is null || _allRestaurants.Count == 0)
        {
            _nearbyRestaurants = new List<Restaurant>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RestaurantsCollection.ItemsSource = null;
                RestaurantsCollection.ItemsSource = _nearbyRestaurants;
            });
            return;
        }

        _nearbyRestaurants = _allRestaurants
            .Select(r => new
            {
                Restaurant = r,
                Distance = GetDistanceInMeters(
                    _currentLocation.Latitude,
                    _currentLocation.Longitude,
                    r.Latitude,
                    r.Longitude)
            })
            .Where(x => x.Distance <= Math.Max(x.Restaurant.GeofenceRadius, NearbyRadiusMeters))
            .OrderBy(x => x.Distance)
            .Take(MaxNearbyRestaurants)
            .Select(x => x.Restaurant)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RestaurantsCollection.ItemsSource = null;
            RestaurantsCollection.ItemsSource = _nearbyRestaurants;
        });
    }

	private static double GetDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
	{
		const double earthRadius = 6371000;
		var dLat = (lat2 - lat1) * Math.PI / 180;
		var dLon = (lon2 - lon1) * Math.PI / 180;

		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
				Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return earthRadius * c;
	}
}