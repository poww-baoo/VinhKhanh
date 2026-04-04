using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class TrackingPage : ContentPage
{
	private Location? _currentLocation;
	private List<Restaurant> _restaurants = new();
	private bool _isTrackingEnabled = false;
	private LocationService? _locationService;
	private readonly LocalizationService _localizationService;

	public TrackingPage()
	{
		InitializeComponent();
		_localizationService = LocalizationService.Instance;
		_localizationService.LanguageChanged += OnLanguageChangedEvent;
		RestaurantsCollection.ItemsSource = _restaurants;
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
		_restaurants = restaurants;
		RestaurantsCollection.ItemsSource = _restaurants;
	}

	public void SetLocationService(LocationService locationService)
	{
		_locationService = locationService;
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
				await _locationService.StartTrackingAsync(_restaurants);
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
}