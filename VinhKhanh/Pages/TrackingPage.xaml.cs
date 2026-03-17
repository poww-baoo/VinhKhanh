using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class TrackingPage : ContentPage
{
	private Location? _currentLocation;
	private List<Restaurant> _restaurants = new();
	private bool _isTrackingEnabled = false;
	private LocationService? _locationService;

	public TrackingPage()
	{
		InitializeComponent();
		RestaurantsCollection.ItemsSource = _restaurants;
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
		if (_isTrackingEnabled)
		{
			TrackingToggleButton.Text = "⏸️ Tắt Theo Dõi";
			TrackingToggleButton.BackgroundColor = Color.FromArgb("#E74C3C");
			StatusLabel.Text = "Đang theo dõi vị trí...";
		}
		else
		{
			TrackingToggleButton.Text = "▶️ Bật Theo Dõi";
			TrackingToggleButton.BackgroundColor = Color.FromArgb("#FF6B35");
			StatusLabel.Text = "Sẵn sàng";
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
				DisplayAlert("Thành công", "Đã bật theo dõi vị trí", "OK");
				});
			}
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Lỗi", $"Không thể bật theo dõi: {ex.Message}", "OK");
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
					DisplayAlert("Thành công", "Đã tắt theo dõi vị trí", "OK");
				});
			}
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Lỗi", $"Không thể tắt theo dõi: {ex.Message}", "OK");
			});
		}
	}

	public void UpdateLocation(Location location)
	{
		try
		{
			_currentLocation = location;
			LocationLabel.Text = $"Vĩ độ: {location.Latitude:F4}, Kinh độ: {location.Longitude:F4}";
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				DisplayAlert("Lỗi", $"Lỗi cập nhật vị trí: {ex.Message}", "OK");
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
				DisplayAlert("Lỗi", $"Lỗi cập nhật trạng thái: {ex.Message}", "OK");
			});
		}
	}
}