using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class TrackingPage : ContentPage
{
	private readonly LocationService _locationService;
	private bool _isTracking = false;
	private List<Restaurant> _restaurants = new();

	public TrackingPage()
	{
		InitializeComponent();
		_locationService = new LocationService();
	}

	public void SetRestaurants(List<Restaurant> restaurants)
	{
		_restaurants = restaurants;
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

			// Không await vòng lặp tracking dài hạn để tránh UI bị giữ handler
			_ = _locationService.StartTrackingAsync(_restaurants);
			return;
		}

		TrackingButton.Text = "▶️ BẮT ĐẦU THEO DÕI";
		TrackingButton.BackgroundColor = Color.FromArgb("#FF6B35");
		StatusLabel.Text = "Nhấn để bắt đầu theo dõi";
		TrackingStatusLabel.Text = "Trạng thái: Chưa bắt đầu";
		_locationService.StopTracking();

		await Task.CompletedTask;
	}

	public void UpdateLocation(Location location)
	{
		StatusLabel.Text = $"Vị trí: {location.Latitude:F4}, {location.Longitude:F4}";
	}

	public void UpdateStatus(string status)
	{
		StatusLabel.Text = status;
	}
}