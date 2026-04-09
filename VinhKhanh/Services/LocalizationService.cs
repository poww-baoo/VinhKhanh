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
                    { "Tracking_Status_Approaching", "Sắp đến" },
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
                    { "RestaurantDetailTitle", "Chi Tiết Quán Ăn" },
                    { "History", "Lịch Sử" },
                    { "Introduction", "Giới thiệu" },
                    { "FeaturedMenu", "Menu Nổi Bật" },
                    { "NoMenuData", "Chưa có dữ liệu menu" },
                    { "PlaybackControls", "Điều Khiển Phát" },
                    { "ListenStoryButton", "🔊 Nghe Câu Chuyện" },
                    { "Zh", "中文" },
                    { "Ja", "日本語" },
                    { "Ru", "Русский" },
                    { "Fr", "Français" },
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
                    { "Tracking_Status_Approaching", "Approaching" },
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
                    { "RestaurantDetailTitle", "Restaurant Details" },
                    { "History", "History" },
                    { "Introduction", "Introduction" },
                    { "FeaturedMenu", "Featured Menu" },
                    { "NoMenuData", "No menu data available" },
                    { "PlaybackControls", "Playback Controls" },
                    { "ListenStoryButton", "🔊 Listen to Story" },
                    { "Zh", "Chinese" },
                    { "Ja", "Japanese" },
                    { "Ru", "Russian" },
                    { "Fr", "French" },
                }
            },
            {
                "zh", new Dictionary<string, string>
                {
                    { "Settings", "设置" },
                    { "Language", "🌐 语言" },
                    { "Notifications", "🔔 通知" },
                    { "NotificationDesc", "靠近餐厅时接收通知" },
                    { "About", "ℹ️ 关于" },
                    { "AppName", "Vinh Khanh 街头美食" },
                    { "Version", "版本: 1.0.0" },
                    { "Explore", "探索" },
                    { "Saved", "已保存" },
                    { "Tracking", "追踪" },
                    { "QRCode", "扫码" },
                    { "SearchPlaceholder", "搜索餐厅..." },
                    { "Results", "结果" },
                    { "ViewDetails", "详情" },
                    { "Play", "播放" },
                    { "Categories", "分类" },
                    { "RestaurantsNearYou", "附近餐厅" },
                    { "CurrentLocation", "当前位置:" },
                    { "YearEstablished", "成立年份" },
                    { "SignatureDish", "招牌菜" },
                    { "Address", "地址" },
                    { "NarrationLanguage", "讲解语言" },
                    { "History", "历史" },
                    { "Introduction", "介绍" },
                    { "FeaturedMenu", "特色菜单" },
                    { "PlaybackControls", "播放控制" },
                    { "ListenStoryButton", "🔊 收听故事" },
                    { "NoMenuData", "暂无菜单数据" },
                    { "Error", "错误" },
                    { "OK", "确定" },
                    { "Vi", "Tiếng Việt" },
                    { "En", "English" },
                    { "Zh", "中文" },
                    { "Ja", "日本語" },
                    { "Ru", "Русский" },
                    { "Fr", "Français" },
                }
            },
            {
                "ja", new Dictionary<string, string>
                {
                    { "Settings", "設定" },
                    { "Language", "🌐 言語" },
                    { "Notifications", "🔔 通知" },
                    { "NotificationDesc", "レストランの近くで通知を受け取る" },
                    { "About", "ℹ️ 情報" },
                    { "AppName", "Vinh Khanh ストリートフード" },
                    { "Version", "バージョン: 1.0.0" },
                    { "Explore", "探索" },
                    { "Saved", "保存済み" },
                    { "Tracking", "追跡" },
                    { "QRCode", "QRスキャン" },
                    { "SearchPlaceholder", "レストランを検索..." },
                    { "Results", "件" },
                    { "ViewDetails", "詳細" },
                    { "Play", "再生" },
                    { "Categories", "カテゴリー" },
                    { "RestaurantsNearYou", "近くのレストラン" },
                    { "CurrentLocation", "現在地:" },
                    { "YearEstablished", "創業年" },
                    { "SignatureDish", "看板料理" },
                    { "Address", "住所" },
                    { "NarrationLanguage", "ナレーション言語" },
                    { "History", "歴史" },
                    { "Introduction", "紹介" },
                    { "FeaturedMenu", "おすすめメニュー" },
                    { "PlaybackControls", "再生コントロール" },
                    { "ListenStoryButton", "🔊 ストーリーを聴く" },
                    { "NoMenuData", "メニューデータがありません" },
                    { "Error", "エラー" },
                    { "OK", "OK" },
                    { "Vi", "Tiếng Việt" },
                    { "En", "English" },
                    { "Zh", "中文" },
                    { "Ja", "日本語" },
                    { "Ru", "Русский" },
                    { "Fr", "Français" },
                }
            },
            {
                "ru", new Dictionary<string, string>
                {
                    { "Settings", "Настройки" },
                    { "Language", "🌐 Язык" },
                    { "Notifications", "🔔 Уведомления" },
                    { "NotificationDesc", "Получать уведомления рядом с рестораном" },
                    { "About", "ℹ️ О приложении" },
                    { "AppName", "Уличная еда Винькхань" },
                    { "Version", "Версия: 1.0.0" },
                    { "Explore", "Обзор" },
                    { "Saved", "Сохранено" },
                    { "Tracking", "Отслеживание" },
                    { "QRCode", "QR-сканер" },
                    { "SearchPlaceholder", "Поиск ресторанов..." },
                    { "Results", "результатов" },
                    { "ViewDetails", "Подробнее" },
                    { "Play", "Слушать" },
                    { "Categories", "Категории" },
                    { "RestaurantsNearYou", "Рестораны рядом" },
                    { "CurrentLocation", "Текущее местоположение:" },
                    { "YearEstablished", "Год основания" },
                    { "SignatureDish", "Фирменное блюдо" },
                    { "Address", "Адрес" },
                    { "NarrationLanguage", "Язык озвучки" },
                    { "History", "История" },
                    { "Introduction", "Введение" },
                    { "FeaturedMenu", "Рекомендуемое меню" },
                    { "PlaybackControls", "Управление воспроизведением" },
                    { "ListenStoryButton", "🔊 Слушать историю" },
                    { "NoMenuData", "Нет данных меню" },
                    { "Error", "Ошибка" },
                    { "OK", "OK" },
                    { "Vi", "Tiếng Việt" },
                    { "En", "English" },
                    { "Zh", "中文" },
                    { "Ja", "日本語" },
                    { "Ru", "Русский" },
                    { "Fr", "Français" },
                }
            },
            {
                "fr", new Dictionary<string, string>
                {
                    { "Settings", "Paramètres" },
                    { "Language", "🌐 Langue" },
                    { "Notifications", "🔔 Notifications" },
                    { "NotificationDesc", "Recevoir des notifications près des restaurants" },
                    { "About", "ℹ️ À propos" },
                    { "AppName", "Street Food Vinh Khanh" },
                    { "Version", "Version : 1.0.0" },
                    { "Explore", "Explorer" },
                    { "Saved", "Enregistré" },
                    { "Tracking", "Suivi" },
                    { "QRCode", "Scanner QR" },
                    { "SearchPlaceholder", "Rechercher des restaurants..." },
                    { "Results", "résultats" },
                    { "ViewDetails", "Détails" },
                    { "Play", "Écouter" },
                    { "Categories", "Catégories" },
                    { "RestaurantsNearYou", "Restaurants près de vous" },
                    { "CurrentLocation", "Position actuelle :" },
                    { "YearEstablished", "Fondé en" },
                    { "SignatureDish", "Plat signature" },
                    { "Address", "Adresse" },
                    { "NarrationLanguage", "Langue de narration" },
                    { "History", "Histoire" },
                    { "Introduction", "Introduction" },
                    { "FeaturedMenu", "Menu vedette" },
                    { "PlaybackControls", "Contrôles de lecture" },
                    { "ListenStoryButton", "🔊 Écouter l'histoire" },
                    { "NoMenuData", "Aucune donnée de menu" },
                    { "Error", "Erreur" },
                    { "OK", "OK" },
                    { "Vi", "Tiếng Việt" },
                    { "En", "English" },
                    { "Zh", "中文" },
                    { "Ja", "日本語" },
                    { "Ru", "Русский" },
                    { "Fr", "Français" },
                }
            }
        };

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                var normalized = NormalizeLanguageCode(value);
                if (_currentLanguage != normalized)
                {
                    _currentLanguage = normalized;
                    CultureInfo.CurrentUICulture = new CultureInfo(normalized);
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string GetString(string key, string? language = null)
        {
            language = NormalizeLanguageCode(language ?? CurrentLanguage);

            if (Translations.TryGetValue(language, out var langDict) &&
                langDict.TryGetValue(key, out var langValue))
            {
                return langValue;
            }

            if (Translations.TryGetValue("en", out var enDict) &&
                enDict.TryGetValue(key, out var enValue))
            {
                return enValue;
            }

            if (Translations.TryGetValue("vi", out var viDict) &&
                viDict.TryGetValue(key, out var viValue))
            {
                return viValue;
            }

            return key;
        }

        public List<string> SupportedLanguages => new() { "vi", "en", "zh", "ja", "ru", "fr" };

        public string NormalizeLanguageCode(string? language)
        {
            var normalized = (language ?? "vi").Trim().ToLowerInvariant();
            return normalized switch
            {
                "jp" => "ja",
                _ => normalized
            };
        }

        public string GetLanguageDisplayName(string language)
        {
            var normalized = NormalizeLanguageCode(language);
            return normalized switch
            {
                "vi" => "🇻🇳 Tiếng Việt",
                "en" => "🇬🇧 English",
                "zh" => "🇨🇳 中文",
                "ja" => "🇯🇵 日本語",
                "ru" => "🇷🇺 Русский",
                "fr" => "🇫🇷 Français",
                _ => normalized
            };
        }
    }
}