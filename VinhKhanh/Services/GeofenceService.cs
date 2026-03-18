using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class GeofenceService
{
    public double CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double EarthRadius = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadius * c;
    }

    public bool IsInsideGeofence(Location userLocation, Restaurant restaurant)
    {
        var distance = CalculateDistanceMeters(
            userLocation.Latitude,
            userLocation.Longitude,
            restaurant.Latitude,
            restaurant.Longitude);

        return distance <= restaurant.GeofenceRadius;
    }

    private static double DegreesToRadians(double degrees) =>
        degrees * Math.PI / 180.0;
}