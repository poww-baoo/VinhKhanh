using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.Models;

namespace VinhKhanh.Services
{
    public class LocationService
    {
        private CancellationTokenSource _cts;
        private readonly double _debounceSeconds = 3;
        private DateTime _lastLocationUpdate = DateTime.MinValue;
        private readonly LocalizationService _localizationService = LocalizationService.Instance;
        private readonly object _restaurantsLock = new();
        private List<Restaurant> _trackedRestaurants = new();

        public event EventHandler<Location> LocationUpdated;
        public event EventHandler<Restaurant> EnteredGeofence;
        public event EventHandler<Restaurant> ExitedGeofence;

        private Dictionary<string, DateTime> _geofenceEntryTimes = new();
        private Dictionary<string, DateTime> _lastPlaybackTimes = new();
        private bool _isFirstLocationUpdate = true;

        public void SetRestaurants(IEnumerable<Restaurant>? restaurants)
        {
            var updated = restaurants?
                .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.Id))
                .ToList() ?? new List<Restaurant>();

            lock (_restaurantsLock)
            {
                _trackedRestaurants = updated;
            }
        }

        private List<Restaurant> GetRestaurantsSnapshot()
        {
            lock (_restaurantsLock)
            {
                return _trackedRestaurants.ToList();
            }
        }

        public async Task StartTrackingAsync(List<Restaurant> restaurants)
        {
            if (IsBusy)
                return;

            SetRestaurants(restaurants);

            try
            {
                var status = await CheckLocationPermission();
                if (status != PermissionStatus.Granted)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var language = _localizationService.CurrentLanguage;
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                _localizationService.GetString("PermissionError", language),
                                _localizationService.GetString("LocationPermissionDenied", language),
                                _localizationService.GetString("OK", language));
                        }
                    });
                    return;
                }

                IsBusy = true;
                _cts = new CancellationTokenSource();
                _isFirstLocationUpdate = true;

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var location = await GetCurrentLocationAsync();
                        if (location != null)
                        {
                            // Lần đầu tiên cập nhật location ngay lập tức, sau đó có debounce
                            bool shouldUpdate = _isFirstLocationUpdate ||
                                               (DateTime.Now - _lastLocationUpdate).TotalSeconds >= _debounceSeconds;

                            if (shouldUpdate)
                            {
                                LocationUpdated?.Invoke(this, location);
                                _lastLocationUpdate = DateTime.Now;
                                _isFirstLocationUpdate = false;

                                var restaurantsSnapshot = GetRestaurantsSnapshot();
                                if (restaurantsSnapshot.Count > 0)
                                {
                                    CheckGeofences(location, restaurantsSnapshot);
                                }
                            }
                        }

                        await Task.Delay(2000, _cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Location tracking error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var language = _localizationService.CurrentLanguage;
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            _localizationService.GetString("Error", language),
                            $"{_localizationService.GetString("LocationTrackingError", language)}: {ex.Message}",
                            _localizationService.GetString("OK", language));
                    }
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void StopTracking()
        {
            _cts?.Cancel();
            IsBusy = false;
            _isFirstLocationUpdate = true;
        }

        private void CheckGeofences(Location userLocation, List<Restaurant> restaurants)
        {
            foreach (var restaurant in restaurants)
            {
                double distance = GetDistance(
                    userLocation.Latitude, userLocation.Longitude,
                    restaurant.Latitude, restaurant.Longitude
                );

                bool isInside = distance <= restaurant.GeofenceRadius;
                string restaurantId = restaurant.Id;

                if (isInside)
                {
                    if (!_geofenceEntryTimes.ContainsKey(restaurantId))
                    {
                        _geofenceEntryTimes[restaurantId] = DateTime.Now;
                    }

                    if ((DateTime.Now - _geofenceEntryTimes[restaurantId]).TotalSeconds >= _debounceSeconds)
                    {
                        if (!_lastPlaybackTimes.ContainsKey(restaurantId) ||
                            (DateTime.Now - _lastPlaybackTimes[restaurantId]).TotalMinutes >= 20)
                        {
                            EnteredGeofence?.Invoke(this, restaurant);
                            _lastPlaybackTimes[restaurantId] = DateTime.Now;
                        }
                    }
                }
                else
                {
                    if (_geofenceEntryTimes.ContainsKey(restaurantId))
                    {
                        _geofenceEntryTimes.Remove(restaurantId);
                        ExitedGeofence?.Invoke(this, restaurant);
                    }
                }
            }
        }

        private async Task<Location> GetCurrentLocationAsync()
        {
            return await Geolocation.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.High,
                    timeout: TimeSpan.FromSeconds(10)
                )
            );
        }

        private async Task<PermissionStatus> CheckLocationPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status;
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public bool IsBusy { get; set; }
    }
}