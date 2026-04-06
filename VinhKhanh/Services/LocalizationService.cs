using System.Globalization;

namespace VinhKhanh.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private string _currentLanguage = "vi";

        public event EventHandler? LanguageChanged;

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            {
                "vi", new Dictionary<string, string>
                {
                    { "Settings", "Cài Đặt" },
                    { "Language", "🌐 Ngôn Ngữ" },
                    { "Vietnamese", "🇻🇳 Tiếng Việt" },
                    { "English", "🇬🇧 English" },
                    { "Notifications", "🔔 Thông Báo" },
                    { "NotificationDesc", "Nhận thông báo khi gần quán ăn" },
                    { "About", "ℹ️ Thông Tin" },
                    { "AppName", "Phố Ẩm Thực Vĩnh Khánh" },
                    { "Version", "Phiên bản: 1.0.0" },
                    { "Explore", "Khám Phá" },
                    { "StreetFood", "Phố Ẩm Thực" },
                    { "Vinh", "Vĩnh" },
                    { "Khanh", "Khánh" },
                    { "SearchPlaceholder", "Tìm kiếm quán ăn..." },
                    { "Results", "kết quả" },
                    { "ViewDetails", "Chi tiết" },
                    { "Play", "Nghe" },
                    { "MapNotLoaded", "Không tải được Map SDK." },
                    { "MapArea", "Bản đồ khu vực" },
                    { "Categories", "Danh mục" },
                    { "RestaurantsNearYou", "Quán ăn gần bạn" },
                    { "CurrentLocation", "Vị trí hiện tại:" },
                    { "District1HCMC", "Quận 1, TP. HCM" },
                    { "YearEstablished", "Thành lập" },
                    { "SignatureDish", "Món Đặc Biệt" },
                    { "Menu", "Thực Đơn" },
                    { "Audio", "Âm Thanh" },
                    { "Address", "Địa chỉ" },
                    { "NarrationLanguage", "Ngôn ngữ thuyết minh" },
                    { "Vi", "Tiếng Việt" },
                    { "En", "English" },
                    { "SaveRestaurant", "💾 Lưu Quán" },
                    { "SavedRestaurant", "✓ Đã Lưu" },
                    { "RemoveFromSaved", "❌ Bỏ Lưu" },
                    { "SaveSuccess", "Lưu thành công" },
                    { "RemoveSuccess", "Bỏ lưu thành công" },
                    { "SaveError", "Lỗi khi lưu" },
                    { "RemoveError", "Lỗi khi bỏ lưu" },
                    { "Success", "Thành công" },
                    { "Saved", "Đã Lưu" },
                    { "Tracking", "Theo Dõi" },
                    { "QRCode", "Quét QR" },
                    { "DataError", "Lỗi dữ liệu" },
                    { "CannotLoadData", "Không thể đọc dữ liệu" },
                    { "Error", "Lỗi" },
                    { "CannotLoadPage", "Không thể tải trang" },
                    { "OK", "Được" },
                    { "Tracking_Status_Completed", "Phát thuyết minh xong" },
                    { "Tracking_Status_Playing", "Đang phát thuyết minh" },
                    { "NoSavedRestaurants", "Chưa có quán ăn nào được lưu" },
                    { "SavedRestaurantsLabel", "📍 Đã lưu" },
                    { "ListenButton", "🔊 Nghe" },
                    { "DetailsButton", "📋 Chi tiết" },
                    { "ScanQR", "Quét Mã QR" },
                    { "QRInstruction", "📱 Hướng camera vào mã QR" },
                    { "QRDetected", "Quét Mã QR Thành Công" },
                    { "QRResult", "Kết quả" },
                    { "CameraPermissionError", "Ứng dụng cần quyền truy cập camera để quét mã QR" },
                    { "PermissionError", "Lỗi yêu cầu quyền" },
                    { "PermissionUnknownError", "Không thể xác định trạng thái quyền camera. Vui lòng kiểm tra cài đặt ứng dụng." },
                    { "FlashlightNotSupported", "Thiết bị không hỗ trợ đèn pin" },
                    { "RestaurantNotFound", "❌ Không tìm thấy nhà hàng" },
                    { "ScanOtherQR", "Vui lòng quét mã QR khác" },
                    { "LoadRestaurantError", "❌ Lỗi khi tải dữ liệu nhà hàng:" },
                    { "StartTracking", "▶️ Bật Theo Dõi" },
                    { "StopTracking", "⏸️ Tắt Theo Dõi" },
                    { "CurrentLocationTracking", "📍 Vị Trí Hiện Tại" },
                    { "WaitingGPS", "Chờ GPS..." },
                    { "Status", "🔊 Trạng Thái" },
                    { "Ready", "Sẵn sàng" },
                    { "Tracking_Enabled", "Đang theo dõi vị trí..." },
                    { "TrackingSuccess", "Thành công" },
                    { "TrackingStarted", "Đã bật theo dõi vị trí" },
                    { "TrackingStopped", "Đã tắt theo dõi vị trí" },
                    { "TrackingError", "Không thể" },
                    { "StartTrackingError", "Không thể bật theo dõi" },
                    { "StopTrackingError", "Không thể tắt theo dõi" },
                    { "LocationError", "Lỗi cập nhật vị trí" },
                    { "StatusError", "Lỗi cập nhật trạng thái" },
                    { "NearbyRestaurants", "🍽️ Quán Ăn Gần Bạn" },
                    { "Latitude", "Vĩ độ" },
                    { "Longitude", "Kinh độ" },
                    { "LocationPermissionDenied", "Quyền vị trí bị từ chối" },
                    { "LocationTrackingError", "Lỗi theo dõi vị trí" },
                }
            },
            {
                "en", new Dictionary<string, string>
                {
                    { "Settings", "Settings" },
                    { "Language", "🌐 Language" },
                    { "Vietnamese", "🇻🇳 Vietnamese" },
                    { "English", "🇬🇧 English" },
                    { "Notifications", "🔔 Notifications" },
                    { "NotificationDesc", "Receive notifications when near a restaurant" },
                    { "About", "ℹ️ About" },
                    { "AppName", "Vinh Khanh Street Food" },
                    { "Version", "Version: 1.0.0" },
                    { "Explore", "Explore" },
                    { "StreetFood", "Street Food" },
                    { "Vinh", "Vinh" },
                    { "Khanh", "Khanh" },
                    { "SearchPlaceholder", "Search restaurants..." },
                    { "Results", "results" },
                    { "ViewDetails", "Details" },
                    { "Play", "Listen" },
                    { "MapNotLoaded", "Could not load Map SDK." },
                    { "MapArea", "Area Map" },
                    { "Categories", "Categories" },
                    { "RestaurantsNearYou", "Restaurants near you" },
                    { "CurrentLocation", "Current location:" },
                    { "District1HCMC", "District 1, Ho Chi Minh City" },
                    { "YearEstablished", "Established" },
                    { "SignatureDish", "Signature Dish" },
                    { "Menu", "Menu" },
                    { "Audio", "Audio" },
                    { "Address", "Address" },
                    { "NarrationLanguage", "Narration language" },
                    { "Vi", "Vietnamese" },
                    { "En", "English" },
                    { "SaveRestaurant", "💾 Save Restaurant" },
                    { "SavedRestaurant", "✓ Saved" },
                    { "RemoveFromSaved", "❌ Remove" },
                    { "SaveSuccess", "Saved successfully" },
                    { "RemoveSuccess", "Removed successfully" },
                    { "SaveError", "Error saving" },
                    { "RemoveError", "Error removing" },
                    { "Success", "Success" },
                    { "Saved", "Saved" },
                    { "Tracking", "Tracking" },
                    { "QRCode", "Scan QR" },
                    { "DataError", "Data Error" },
                    { "CannotLoadData", "Cannot read data" },
                    { "Error", "Error" },
                    { "CannotLoadPage", "Cannot load page" },
                    { "OK", "OK" },
                    { "Tracking_Status_Completed", "Playback completed" },
                    { "Tracking_Status_Playing", "Now playing narration" },
                    { "NoSavedRestaurants", "No saved restaurants yet" },
                    { "SavedRestaurantsLabel", "📍 Saved" },
                    { "ListenButton", "🔊 Listen" },
                    { "DetailsButton", "📋 Details" },
                    { "ScanQR", "Scan QR Code" },
                    { "QRInstruction", "📱 Point camera at QR code" },
                    { "QRDetected", "QR Code Scanned Successfully" },
                    { "QRResult", "Result" },
                    { "CameraPermissionError", "The application needs camera access permission to scan QR codes" },
                    { "PermissionError", "Permission Error" },
                    { "PermissionUnknownError", "Cannot determine camera permission status. Please check application settings." },
                    { "FlashlightNotSupported", "Device does not support flashlight" },
                    { "RestaurantNotFound", "❌ Restaurant not found" },
                    { "ScanOtherQR", "Please scan another QR code" },
                    { "LoadRestaurantError", "❌ Error loading restaurant data:" },
                    { "StartTracking", "▶️ Start Tracking" },
                    { "StopTracking", "⏸️ Stop Tracking" },
                    { "CurrentLocationTracking", "📍 Current Location" },
                    { "WaitingGPS", "Waiting for GPS..." },
                    { "Status", "🔊 Status" },
                    { "Ready", "Ready" },
                    { "Tracking_Enabled", "Tracking location..." },
                    { "TrackingSuccess", "Success" },
                    { "TrackingStarted", "Location tracking enabled" },
                    { "TrackingStopped", "Location tracking disabled" },
                    { "TrackingError", "Error" },
                    { "StartTrackingError", "Cannot enable tracking" },
                    { "StopTrackingError", "Cannot disable tracking" },
                    { "LocationError", "Location update error" },
                    { "StatusError", "Status update error" },
                    { "NearbyRestaurants", "🍽️ Nearby Restaurants" },
                    { "Latitude", "Latitude" },
                    { "Longitude", "Longitude" },
                    { "LocationPermissionDenied", "Location permission was denied" },
                    { "LocationTrackingError", "Location tracking error" },
                }
            }
        };

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    CultureInfo.CurrentUICulture = new CultureInfo(value);
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string GetString(string key, string? language = null)
        {
            language ??= CurrentLanguage;

            if (Translations.TryGetValue(language, out var languageDict) &&
                languageDict.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
        }

        public List<string> SupportedLanguages => new() { "vi", "en", "zh", "ja" };
    }
}