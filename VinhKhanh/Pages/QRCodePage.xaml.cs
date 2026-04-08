using System.Diagnostics;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    /// <summary>
    /// Trang quét mã QR để tìm kiếm nhà hàng
    /// 
    /// Luồng hoạt động:
    /// 1. Khởi động camera và yêu cầu quyền truy cập
    /// 2. Quét mã QR chứa ID của nhà hàng (ví dụ: "1", "2", "3")
    /// 3. Tìm kiếm nhà hàng tương ứng từ cơ sở dữ liệu
    /// 4. Chuyển trang đến RestaurantDetailPage với toàn bộ dữ liệu nhà hàng
    /// </summary>
    public partial class QRCodePage : ContentPage
    {
        // ============ BIẾN PRIVATE ============
        /// <summary>Lưu trữ kết quả quét mã QR cuối cùng để tránh quét lặp lại</summary>
        private string? _lastScannedResult;

        /// <summary>
        /// Cờ kiểm soát xử lý để tránh xử lý nhiều mã QR cùng lúc
        /// Khi đang xử lý mã này, không xử lý mã tiếp theo
        /// </summary>
        private bool _isProcessing = false;

        /// <summary>Dịch vụ đa ngôn ngữ (Tiếng Việt, Tiếng Anh)</summary>
        private readonly LocalizationService _localizationService;

        /// <summary>Dịch vụ quét mã QR và tìm kiếm nhà hàng</summary>
        private readonly QRCodeService _qrCodeService;

        /// <summary>Dịch vụ phát nhạc nền</summary>
        private readonly AudioPlaybackService _audioPlaybackService;

        // ============ CONSTRUCTOR ============
        /// <summary>
        /// Khởi tạo trang quét mã QR
        /// - Thiết lập các dịch vụ cần thiết
        /// - Đăng ký sự kiện thay đổi ngôn ngữ
        /// - Cập nhật giao diện
        /// </summary>
        public QRCodePage()
        {
            InitializeComponent();

            // Khởi tạo các dịch vụ
            _localizationService = LocalizationService.Instance;
            _qrCodeService = ResolveService<QRCodeService>() ?? new QRCodeService(new DatabaseService());
            _audioPlaybackService = ResolveService<AudioPlaybackService>() ?? new AudioPlaybackService();

            // Đăng ký sự kiện thay đổi ngôn ngữ
            _localizationService.LanguageChanged += OnLanguageChangedEvent;

            // Cập nhật giao diện lần đầu
            UpdateUI();
            ConfigureScanner();

            Debug.WriteLine("QRCodePage: Trang QR Code đã được khởi tạo");
        }

        /// <summary>
        /// Hỗ trợ giải quyết dịch vụ từ DI Container
        /// </summary>
        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        // ============ SỰ KIỆN VÒNG ĐỜI (LIFECYCLE) ============
        /// <summary>
        /// Được gọi khi trang xuất hiện trên màn hình
        /// - Bật camera quét mã QR
        /// - Yêu cầu quyền truy cập camera
        /// 
        /// QUAN TRỌNG: Delay 700ms để đảm bảo camera ready
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                Debug.WriteLine("QRCodePage.OnAppearing: Trang đang xuất hiện...");

                _isProcessing = false;
                _lastScannedResult = null;

                var hasPermission = await RequestCameraPermissionAsync();
                if (!hasPermission)
                {
                    QRScannerView.IsDetecting = false;
                    return;
                }

                // máy yếu cần thêm thời gian init camera
                await Task.Delay(700);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ConfigureScanner();
                    QRScannerView.IsDetecting = true;
                    Debug.WriteLine("QRCodePage: Camera được bật + scanner configured");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.OnAppearing: Lỗi - {ex.Message}");
            }
        }

        /// <summary>
        /// Được gọi khi trang biến mất khỏi màn hình
        /// - Tắt máy quét mã QR
        /// - Giải phóng tài nguyên camera
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                Debug.WriteLine("QRCodePage.OnDisappearing: Trang đang ẩn...");

                QRScannerView.IsDetecting = false;

                Debug.WriteLine("QRCodePage: Camera chuyển sang trạng thái nghỉ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.OnDisappearing: Lỗi - {ex.Message}");
            }
        }

        // ============ QUẢN LÝ NGÔN NGỮ ============
        /// <summary>
        /// Sự kiện được gọi khi người dùng thay đổi ngôn ngữ
        /// - Cập nhật toàn bộ giao diện theo ngôn ngữ mới
        /// </summary>
        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            UpdateUI();
            Debug.WriteLine($"QRCodePage: Ngôn ngữ đã thay đổi");
        }

        /// <summary>
        /// Cập nhật giao diện theo ngôn ngữ hiện tại
        /// - Cập nhật tiêu đề trang
        /// - Cập nhật label hướng dẫn
        /// </summary>
        private void UpdateUI()
        {
            var language = _localizationService.CurrentLanguage;
            Title = _localizationService.GetString("ScanQR", language);
            InstructionLabel.Text = _localizationService.GetString("QRInstruction", language);
        }

        // ============ QUẢN LÝ QUYỀN VÀ CAMERA ============
        /// <summary>
        /// Yêu cầu quyền truy cập camera từ hệ điều hành
        /// 
        /// MAUI PermissionStatus chỉ có 3 trạng thái:
        /// - Granted: Quyền được cấp
        /// - Denied: Quyền bị từ chối
        /// - Unknown: Trạng thái không xác định
        /// </summary>
        private async Task<bool> RequestCameraPermissionAsync()
        {
            try
            {
                var language = _localizationService.CurrentLanguage;

                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                Debug.WriteLine($"QRCodePage: Trạng thái quyền camera - {cameraStatus}");

                if (cameraStatus != PermissionStatus.Granted)
                {
                    Debug.WriteLine("QRCodePage: Yêu cầu quyền camera từ người dùng");
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    Debug.WriteLine($"QRCodePage: Người dùng phản hồi - {cameraStatus}");
                }

                if (cameraStatus == PermissionStatus.Granted)
                {
                    Debug.WriteLine("✅ QRCodePage: Quyền camera được cấp");
                    return true;
                }

                if (cameraStatus == PermissionStatus.Denied)
                {
                    Debug.WriteLine("❌ QRCodePage: Quyền camera bị từ chối");
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        _localizationService.GetString("CameraPermissionError", language),
                        _localizationService.GetString("OK", language));
                    return false;
                }

                Debug.WriteLine("❓ QRCodePage: Trạng thái quyền camera không xác định");
                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    _localizationService.GetString("PermissionUnknownError", language),
                    _localizationService.GetString("OK", language));
                return false;
            }
            catch (Exception ex)
            {
                var language = _localizationService.CurrentLanguage;
                Debug.WriteLine($"❌ QRCodePage.RequestCameraPermissionAsync: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("PermissionError", language),
                    $"{_localizationService.GetString("PermissionError", language)}: {ex.Message}",
                    _localizationService.GetString("OK", language));
                return false;
            }
        }

        // ============ XỬ LÝ MÃ QR ============
        /// <summary>
        /// Sự kiện được gọi khi máy quét phát hiện mã vạch/QR
        /// 
        /// Kiểm soát để tránh xử lý lặp lại
        /// </summary>
        private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            try
            {
                if (_isProcessing)
                {
                    return;
                }

                if (e.Results == null || !e.Results.Any())
                {
                    return;
                }

                var barcode = e.Results.FirstOrDefault();
                if (barcode == null)
                {
                    return;
                }

                var result = barcode.Value?.Trim();
                
                if (string.IsNullOrEmpty(result))
                {
                    Debug.WriteLine("QRCodePage: Mã QR trống");
                    return;
                }

                if (result == _lastScannedResult)
                {
                    Debug.WriteLine($"QRCodePage: Mã QR trùng lặp - {result}");
                    return;
                }

                _isProcessing = true;
                _lastScannedResult = result;

                Debug.WriteLine($"✅ QRCodePage: Phát hiện mã QR mới - {result}");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ProcessQRCodeAsync(result);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ QRCodePage.OnBarcodesDetected: Lỗi - {ex.Message}");
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Xử lý mã QR được phát hiện
        /// 
        /// Các bước:
        /// 1. Tắt máy quét để tránh phát hiện lặp lại
        /// 2. Tìm kiếm nhà hàng từ ID trong mã QR
        /// 3. Nếu tìm thấy, chuyển trang đến RestaurantDetailPage
        /// 4. Nếu không tìm thấy, hiển thị thông báo lỗi
        /// 5. Reset trạng thái để quét lại
        /// </summary>
        private async Task ProcessQRCodeAsync(string qrCodeId)
        {
            try
            {
                var language = _localizationService.CurrentLanguage;

                Debug.WriteLine($"🔍 QRCodePage: Đang tìm kiếm nhà hàng với ID: {qrCodeId}");

                QRScannerView.IsDetecting = false;

                var restaurant = await _qrCodeService.GetRestaurantFromIdAsync(qrCodeId);

                if (restaurant != null)
                {
                    Debug.WriteLine($"✅ QRCodePage: Tìm thấy nhà hàng - {restaurant.Name}");

                    await Shell.Current.GoToAsync(
                        $"restaurantdetail?id={restaurant.Id}",
                        new Dictionary<string, object>
                        {
                            { "restaurant", restaurant },
                            { "audioService", _audioPlaybackService }
                        });
                }
                else
                {
                    Debug.WriteLine($"❌ QRCodePage: Không tìm thấy nhà hàng với ID: {qrCodeId}");
                    
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        $"ID: {qrCodeId}\n\n{_localizationService.GetString("RestaurantNotFound", language)}\n\n{_localizationService.GetString("ScanOtherQR", language)}",
                        _localizationService.GetString("OK", language));

                    _isProcessing = false;
                    _lastScannedResult = null;
                    QRScannerView.IsDetecting = true;
                }
            }
            catch (Exception ex)
            {
                var language = _localizationService.CurrentLanguage;
                Debug.WriteLine($"❌ QRCodePage.ProcessQRCodeAsync: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    $"{_localizationService.GetString("LoadRestaurantError", language)}\n{ex.Message}",
                    _localizationService.GetString("OK", language));

                _isProcessing = false;
                _lastScannedResult = null;
                QRScannerView.IsDetecting = true;
            }
        }

        private void ConfigureScanner()
        {
            QRScannerView.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            };

            QRScannerView.CameraLocation = CameraLocation.Rear;
        }
    }
}