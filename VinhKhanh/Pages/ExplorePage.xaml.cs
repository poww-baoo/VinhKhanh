using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class ExplorePage : ContentPage
{
	private AudioPlaybackService _audioService;

	public ExplorePage()
	{
		InitializeComponent();
		_audioService = new AudioPlaybackService();
	}

	public void SetCategories(List<Category> categories)
	{
		FilterChipsCollection.ItemsSource = categories;
	}

	public void SetRestaurants(List<Restaurant> restaurants)
	{
		RestaurantsCollection.ItemsSource = restaurants;
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