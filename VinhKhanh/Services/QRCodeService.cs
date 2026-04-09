using System.Diagnostics;
using System.Text.RegularExpressions;
using VinhKhanh.Models;

namespace VinhKhanh.Services
{
    /// <summary>
    /// Dịch vụ xử lý mã QR
    /// 
    /// Chức năng:
    /// - Nhận ID từ mã QR được quét
    /// - Tìm kiếm nhà hàng từ cơ sở dữ liệu hoặc dữ liệu giả
    /// - Trả về đối tượng Restaurant đầy đủ với tất cả thông tin
    /// </summary>
    public class QRCodeService
    {
        /// <summary>Dịch vụ cơ sở dữ liệu để truy vấn nhà hàng</summary>
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// Khởi tạo dịch vụ QR Code
        /// </summary>
        /// <param name="databaseService">Dịch vụ cơ sở dữ liệu</param>
        public QRCodeService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Tìm kiếm nhà hàng theo ID từ mã QR
        /// 
        /// Luồng:
        /// 1. Kiểm tra ID có hợp lệ không (không trống)
        /// 2. Chuyển đổi ID từ string sang int
        /// 3. Cố gắng tìm từ cơ sở dữ liệu sử dụng GetPoiByIdAsync
        /// 4. Nếu không tìm thấy, trả về dữ liệu giả (mock data)
        /// 5. Nếu vẫn không tìm thấy, trả về null
        /// </summary>
        /// <param name="id">ID của nhà hàng từ mã QR (dạng string)</param>
        /// <returns>Đối tượng Restaurant nếu tìm thấy, null nếu không</returns>
        public async Task<Restaurant?> GetRestaurantFromIdAsync(string id)
        {
            try
            {
                // Bước 1: Kiểm tra ID có hợp lệ không
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.WriteLine($"QRCodeService: ID nhà hàng trống");
                    return null;
                }

                Debug.WriteLine($"QRCodeService: Tìm kiếm nhà hàng với ID gốc - {id}");

                var normalizedId = ExtractPoiId(id);
                if (string.IsNullOrWhiteSpace(normalizedId))
                {
                    Debug.WriteLine($"QRCodeService: Không trích xuất được PoiId từ QR - {id}");
                    return null;
                }

                Debug.WriteLine($"QRCodeService: ID đã chuẩn hóa - {normalizedId}");

                // Bước 2: Chuyển đổi ID từ string sang int
                if (!int.TryParse(normalizedId, out var numericId))
                {
                    Debug.WriteLine($"QRCodeService: ID không phải số - {normalizedId}");
                    // Nếu ID không phải số, thử dữ liệu giả
                    return GetMockRestaurantData(normalizedId);
                }

                // Bước 3: Cố gắng tìm từ cơ sở dữ liệu
                try
                {
                    await _databaseService.InitAsync();
                    var poi = await _databaseService.GetPoiByIdAsync(numericId);

                    if (poi != null)
                    {
                        Debug.WriteLine($"QRCodeService: Tìm thấy nhà hàng trong database - {poi.Name}");
                        return ConvertPoiToRestaurant(poi);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"QRCodeService: Lỗi tìm kiếm trong database - {ex.Message}");
                }

                // Bước 4: Trả về dữ liệu giả nếu không tìm thấy trong database
                Debug.WriteLine($"QRCodeService: Database không có, kiểm tra dữ liệu giả");
                var mockRestaurant = GetMockRestaurantData(normalizedId);

                if (mockRestaurant != null)
                {
                    Debug.WriteLine($"QRCodeService: Tìm thấy nhà hàng trong dữ liệu giả - {mockRestaurant.Name}");
                    return mockRestaurant;
                }

                // Bước 5: Không tìm thấy
                Debug.WriteLine($"QRCodeService: Không tìm thấy nhà hàng với ID {normalizedId}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodeService.GetRestaurantFromIdAsync: Lỗi - {ex.Message}");
                return null;
            }
        }

        private static string? ExtractPoiId(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var value = rawValue.Trim();

            // 1) Dạng đơn giản: "123"
            if (int.TryParse(value, out _))
            {
                return value;
            }

            // 2) Dạng prefix: "poi:123", "poi/123", "restaurant:123"
            var prefixedMatch = Regex.Match(value, @"(?:^|[/:?&=#])(poi|restaurant)[:/=-]?(\d+)(?:$|[/?&#])", RegexOptions.IgnoreCase);
            if (prefixedMatch.Success)
            {
                return prefixedMatch.Groups[2].Value;
            }

            // 3) Dạng query string/url: "...?id=123" hoặc "...?poiId=123"
            var queryMatch = Regex.Match(value, @"(?:\?|&)(?:id|poiid|poi_id|restaurantid|restaurant_id)=(\d+)", RegexOptions.IgnoreCase);
            if (queryMatch.Success)
            {
                return queryMatch.Groups[1].Value;
            }

            // 4) Fallback: lấy cụm số đầu tiên
            var firstNumber = Regex.Match(value, @"\d+");
            return firstNumber.Success ? firstNumber.Value : null;
        }

        /// <summary>
        /// Chuyển đổi đối tượng Poi từ database sang Restaurant
        /// 
        /// Ánh xạ:
        /// - Poi.Id → Restaurant.Id (string)
        /// - Poi.Name → Restaurant.Name
        /// - Poi.Lat/Lng → Restaurant.Latitude/Longitude
        /// - v.v.
        /// </summary>
        /// <param name="poi">Đối tượng Poi từ database</param>
        /// <returns>Đối tượng Restaurant</returns>
        private Restaurant ConvertPoiToRestaurant(Poi poi)
        {
            return new Restaurant
            {
                Id = poi.Id.ToString(),
                CategoryId = poi.CategoryId,
                CategoryName = poi.CategoryName,
                Name = poi.Name,
                YearEstablished = poi.YearEstablished,
                History = poi.History,
                HistoryEn = poi.HistoryEn,
                HistoryJp = poi.HistoryJp,
                HistoryZh = poi.HistoryZh,
                HistoryRu = poi.HistoryRu,
                HistoryFr = poi.HistoryFr,
                Address = poi.Address,
                AdrEn = poi.AdrEn,
                AdrJp = poi.AdrJp,
                AdrZh = poi.AdrZh,
                AdrRu = poi.AdrRu,
                AdrFr = poi.AdrFr,
                TextVi = poi.TextVi,
                TextEn = poi.TextEn,
                TextZh = poi.TextZh,
                TextJa = poi.TextJa,
                TextRu = poi.TextRu,
                TextFr = poi.TextFr,
                Highlights = string.Empty, // Không có trong Poi
                Rating = poi.Rating,
                Latitude = poi.Lat,
                Longitude = poi.Lng,
                GeofenceRadius = poi.RadiusMeters,
                Priority = poi.Priority,
                ImageFileName = poi.ImageFileName,
                SignatureDish = null, // Có thể mở rộng từ database sau
                HighlightMenuItems = null, // Có thể mở rộng từ database sau
                Promotions = null // Có thể mở rộng từ database sau
            };
        }

        /// <summary>
        /// Dữ liệu giả (Mock Data) để test khi database trống
        /// 
        /// Hiện tại có 3 nhà hàng:
        /// - ID "1": Quán Cơm Tấm Truyền Thống
        /// - ID "2": Phở Hoa Mai
        /// - ID "3": Bánh Mỳ Nóng Sốt
        /// 
        /// Có thể thêm/sửa/xóa các nhà hàng ở đây
        /// </summary>
        /// <param name="id">ID nhà hàng cần lấy</param>
        /// <returns>Đối tượng Restaurant nếu tồn tại trong dữ liệu giả</returns>
        private Restaurant? GetMockRestaurantData(string id)
        {
            var mockData = new Dictionary<string, Restaurant>
            {
                // ========== NHÀ HÀNG 1: CƠM TẤM TRUYỀN THỐNG ==========
                {
                    "1", new Restaurant
                    {
                        Id = "1",
                        CategoryId = 1,
                        CategoryName = "Cơm Tấm",
                        Name = "Quán Cơm Tấm Truyền Thống",
                        YearEstablished = 2005,
                        History = "Quán cơm tấm được thành lập từ năm 2005 với truyền thống nấu ăn gia đình, sử dụng những nguyên liệu tươi sống hàng ngày.",
                        TextVi = "Cơm tấm của quán được nấu theo công thức truyền thống, sử dụng những nguyên liệu tươi sống hàng ngày. Sườn nướng mềm thơm, trứng opla có lòng đỏ chảy.",
                        Highlights = "Cơm tấm ngon, giá rẻ, phục vụ nhanh",
                        Rating = 4.5,
                        Latitude = 10.7769,
                        Longitude = 106.6967,
                        Priority = 1,
                        SignatureDish = new SignatureDish
                        {
                            Names = new Dictionary<string, string>
                            {
                                { "vi", "Cơm Tấm Sườn Nướng" },
                                { "en", "Grilled Ribs Broken Rice" }
                            },
                            Stories = new Dictionary<string, string>
                            {
                                { "vi", "Đặc sản của quán, sườn nướng mềm thơm, trứng opla có lòng đỏ chảy" },
                                { "en", "House specialty, tender and aromatic grilled ribs with perfectly cooked egg" }
                            },
                            Reasons = new Dictionary<string, string>
                            {
                                { "vi", "Thịt nướng tự do, không có hóa chất, trứng gia đình tươi mỗi ngày" },
                                { "en", "Natural grilled meat without additives, fresh eggs daily" }
                            }
                        },
                        HighlightMenuItems = new List<RestaurantMenuItem>
                        {
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Cơm Tấm Sườn Nướng" },
                                    { "en", "Grilled Ribs Broken Rice" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Cơm tấm kèm sườn nướng, trứng opla, dưa chua, nước mắm chua" },
                                    { "en", "Broken rice with grilled ribs, egg, pickled vegetables, fish sauce" }
                                }
                            },
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Cơm Tấm Thịt Kho Tàu" },
                                    { "en", "Pork Braised Broken Rice" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Cơm tấm kèm thịt kho tàu, trứng, dưa chua" },
                                    { "en", "Broken rice with braised pork, egg, pickled vegetables" }
                                }
                            },
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Cơm Tấm Cá Kho" },
                                    { "en", "Braised Fish Broken Rice" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Cơm tấm kèm cá kho, trứng, dưa chua" },
                                    { "en", "Broken rice with braised fish, egg, pickled vegetables" }
                                }
                            }
                        },
                        Promotions = new List<Promotion>
                        {
                            new Promotion
                            {
                                Titles = new Dictionary<string, string>
                                {
                                    { "vi", "🎉 Khuyến Mãi Đặc Biệt" },
                                    { "en", "🎉 Special Promotion" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Mua 2 cơm tấm tặng 1 nước ngọt" },
                                    { "en", "Buy 2 broken rice, get 1 soft drink free" }
                                }
                            }
                        }
                    }
                },

                // ========== NHÀ HÀNG 2: PHỞ HOA MAI ==========
                {
                    "2", new Restaurant
                    {
                        Id = "2",
                        CategoryId = 2,
                        CategoryName = "Phở",
                        Name = "Phở Hoa Mai",
                        YearEstablished = 1995,
                        History = "Một trong những quán phở lâu đời nhất tại Vĩnh Khánh, nổi tiếng với nước dùng được nấu từ xương bò trong 12 giờ.",
                        TextVi = "Nước dùng phở được nấu từ xương bò trong 12 giờ cùng với hương vị gia đình độc quyền. Thịt bò được lựa chọn kỹ càng từ những con bò tốt nhất.",
                        Highlights = "Phở thơm ngon, nước dùng đặc biệt, thịt bò mềm",
                        Rating = 4.8,
                        Latitude = 10.7765,
                        Longitude = 106.6960,
                        Priority = 2,
                        SignatureDish = new SignatureDish
                        {
                            Names = new Dictionary<string, string>
                            {
                                { "vi", "Phở Bò" },
                                { "en", "Beef Pho" }
                            },
                            Stories = new Dictionary<string, string>
                            {
                                { "vi", "Phở truyền thống nước dùng đặc biệt được nấu 12 giờ" },
                                { "en", "Traditional pho with special broth simmered 12 hours" }
                            },
                            Reasons = new Dictionary<string, string>
                            {
                                { "vi", "Nước dùng nấu 12 giờ từ xương bò, thịt tươi mỗi sáng" },
                                { "en", "Broth simmered 12 hours from beef bones, fresh meat daily" }
                            }
                        },
                        HighlightMenuItems = new List<RestaurantMenuItem>
                        {
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Phở Bò Tái" },
                                    { "en", "Rare Beef Pho" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Phở với thịt bò tái được nấu vừa chín, ngon nhất" },
                                    { "en", "Pho with rare beef cooked to perfection" }
                                }
                            },
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Phở Bò Nạm" },
                                    { "en", "Brisket Pho" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Phở với thịt bò nạm mềm, nấu kỹ" },
                                    { "en", "Pho with tender brisket beef" }
                                }
                            }
                        }
                    }
                },

                // ========== NHÀ HÀNG 3: BÁNH MÌ NÓNG SỐT ==========
                {
                    "3", new Restaurant
                    {
                        Id = "3",
                        CategoryId = 3,
                        CategoryName = "Bánh Mỳ",
                        Name = "Bánh Mỳ Nóng Sốt",
                        YearEstablished = 2010,
                        History = "Quán bánh mỳ nóng sốt nổi tiếng với công thức độc quyền, bánh giòn rụm với vị sốt đậu phộu ngon mê ly.",
                        TextVi = "Bánh mỳ của quán được nướng nóng mỗi lúc khách đến, sốt đậu phộu tự làm, kèm pâté và thịt lạp xưởng.",
                        Highlights = "Bánh mỳ giòn, sốt đậu phộu ngon, pâté tươi",
                        Rating = 4.3,
                        Latitude = 10.7772,
                        Longitude = 106.6970,
                        Priority = 3,
                        SignatureDish = new SignatureDish
                        {
                            Names = new Dictionary<string, string>
                            {
                                { "vi", "Bánh Mỳ Pâté Xốp" },
                                { "en", "Pâté Crispy Sandwich" }
                            },
                            Stories = new Dictionary<string, string>
                            {
                                { "vi", "Bánh mỳ nóng kèm pâté và sốt đậu phộu tự làm" },
                                { "en", "Hot sandwich with pâté and homemade mayonnaise" }
                            },
                            Reasons = new Dictionary<string, string>
                            {
                                { "vi", "Bánh giòn rụm, pâté tươi, sốt đậu phộu ngon miệng" },
                                { "en", "Crispy bread, fresh pâté, delicious mayonnaise" }
                            }
                        },
                        HighlightMenuItems = new List<RestaurantMenuItem>
                        {
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Bánh Mỳ Pâté Thịt Lạp" },
                                    { "en", "Pâté & Sausage Sandwich" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Bánh mỳ giòn với pâté, thịt lạp xưởng, dưa chua, hành" },
                                    { "en", "Crispy sandwich with pâté, sausage, pickled veggies, onion" }
                                }
                            },
                            new RestaurantMenuItem
                            {
                                Names = new Dictionary<string, string>
                                {
                                    { "vi", "Bánh Mỳ Gà Nướng" },
                                    { "en", "Grilled Chicken Sandwich" }
                                },
                                Descriptions = new Dictionary<string, string>
                                {
                                    { "vi", "Bánh mỳ với gà nướng, sốt đậu phộu, dưa chua" },
                                    { "en", "Sandwich with grilled chicken, mayo, pickled veggies" }
                                }
                            }
                        }
                    }
                }
            };

            // Tìm kiếm nhà hàng theo ID
            if (mockData.TryGetValue(id, out var restaurant))
            {
                return restaurant;
            }

            return null;
        }

        /// <summary>
        /// Tạo nội dung QR code cho một Poi.
        /// Mặc định dùng định dạng "poi:{id}" để dễ mở rộng về sau.
        /// </summary>
        public string BuildPoiQrPayload(int poiId) => $"poi:{poiId}";
    }
}