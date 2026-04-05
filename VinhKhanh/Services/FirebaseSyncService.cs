using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VinhKhanh.Models;

namespace VinhKhanh.Services;

public sealed class FirebaseSyncService
{
    private const string FirebaseBaseUrl = "https://vinhkhanh-f275f-default-rtdb.asia-southeast1.firebasedatabase.app";
    private const string FirebaseRootPath = "vinhkhanh";
    private const string FirebaseVersionKey = "firebase_version";
    private const string FirebasePayloadHashKey = "firebase_payload_hash";

    private readonly DatabaseService _databaseService;
    private readonly ImageSyncService _imageSyncService;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public event EventHandler<int>? SyncCompleted;

    public FirebaseSyncService(DatabaseService databaseService, ImageSyncService imageSyncService)
    {
        _databaseService = databaseService;
        _imageSyncService = imageSyncService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public async Task<bool> SyncIfNeededAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            if (!HasInternet())
            {
                return false;
            }

            await _databaseService.InitAsync();
            var localHasData = await _databaseService.HasAnyPoiAsync();

            var localVersion = Preferences.Get(FirebaseVersionKey, 0);
            var localHash = Preferences.Get(FirebasePayloadHashKey, string.Empty);

            var remoteVersion = await GetRemoteVersionAsync(cancellationToken);
            var payload = await FetchRootPayloadAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var payloadHash = ComputeSha256(payload);

            // Chỉ bỏ qua khi: không force, version không tăng, hash không đổi, và local đã có dữ liệu
            if (!force && remoteVersion <= localVersion &&
                string.Equals(localHash, payloadHash, StringComparison.OrdinalIgnoreCase) &&
                localHasData)
            {
                return false;
            }

            var parsed = ParseFirebasePayload(payload);
            System.Diagnostics.Debug.WriteLine($"[FirebaseSync] Parsed categories={parsed.Categories.Count}, pois={parsed.Pois.Count}, menuItems={parsed.MenuItems.Count}");

            // Không ghi đè local nếu payload không hợp lệ/trống
            if (parsed.Pois.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[FirebaseSync] Skip sync because parsed POI list is empty.");
                return false;
            }

            await _databaseService.ReplaceSeedDataAsync(
                parsed.Categories,
                parsed.Pois,
                parsed.MenuItems);

            foreach (var poi in parsed.Pois)
            {
                if (string.IsNullOrWhiteSpace(poi.ImageUrl))
                {
                    continue;
                }

                await _imageSyncService.DownloadAsync(poi.ImageUrl, poi.ImageFileName, cancellationToken);
            }

            if (remoteVersion > 0)
            {
                Preferences.Set(FirebaseVersionKey, remoteVersion);
                SyncCompleted?.Invoke(this, remoteVersion);
            }
            else
            {
                SyncCompleted?.Invoke(this, localVersion);
            }

            Preferences.Set(FirebasePayloadHashKey, payloadHash);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FirebaseSync] {ex.Message}");
            return false;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private static bool HasInternet()
    {
        try
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FirebaseSync] Connectivity check failed: {ex.Message}");
            return false;
        }
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private async Task<int> GetRemoteVersionAsync(CancellationToken cancellationToken)
    {
        var versionUrl = $"{FirebaseBaseUrl}/{FirebaseRootPath}/meta/version.json";
        using var response = await _httpClient.GetAsync(versionUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        var raw = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (int.TryParse(raw, out var asInt))
        {
            return asInt;
        }

        raw = raw.Trim('"');
        return int.TryParse(raw, out asInt) ? asInt : 0;
    }

    private async Task<string?> FetchRootPayloadAsync(CancellationToken cancellationToken)
    {
        var url = $"{FirebaseBaseUrl}/{FirebaseRootPath}.json";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static ParsedData ParseFirebasePayload(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var categories = ParseCategories(TryGetAny(root, "categories", "Categories"));
        var pois = ParsePois(TryGetAny(root, "pois", "Pois"));
        var menuItems = ParseMenuItems(TryGetAny(root, "menu_items", "menuItems", "MenuItems"));

        return new ParsedData(categories, pois, menuItems);
    }

    private static JsonElement? TryGetAny(JsonElement parent, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            var value = TryGet(parent, name);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static JsonElement? TryGet(JsonElement parent, string propertyName)
    {
        if (parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (parent.TryGetProperty(propertyName, out var value))
        {
            return value;
        }

        foreach (var property in parent.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static List<Category> ParseCategories(JsonElement? element)
    {
        var result = new List<Category>();
        if (!element.HasValue)
        {
            return result;
        }

        foreach (var (item, key) in EnumerateCollection(element.Value))
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var category = new Category
            {
                Id = ReadInt(item, "id", key),
                Name = ReadString(item, "name"),
                IconText = ReadString(item, "iconText", "icon_text", "icon"),
                SortOrder = ReadInt(item, "sortOrder", "sort_order")
            };

            if (!string.IsNullOrWhiteSpace(category.Name))
            {
                result.Add(category);
            }
        }

        return result;
    }

    private static List<Poi> ParsePois(JsonElement? element)
    {
        var result = new List<Poi>();
        if (!element.HasValue)
        {
            return result;
        }

        foreach (var (item, key) in EnumerateCollection(element.Value))
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var poi = new Poi
            {
                Id = ReadInt(item, "id", key),
                CategoryId = ReadInt(item, "categoryId", "category_id"),
                Name = ReadString(item, "name"),
                History = ReadString(item, "history"),
                Address = ReadString(item, "address"),
                TextVi = ReadString(item, "textVi", "text_vi"),
                TextEn = ReadString(item, "textEn", "text_en"),
                TextZh = ReadString(item, "textZh", "text_zh"),
                TextJp = ReadString(item, "textJp", "text_jp", "textJa", "text_ja"),
                TextRu = ReadString(item, "textRu", "text_ru"),
                TextFr = ReadString(item, "textFr", "text_fr"),
                Lat = ReadDouble(item, "lat", "latitude"),
                Lng = ReadDouble(item, "lng", "longitude"),
                RadiusMeters = ReadDouble(item, "radiusMeters", "radius_meters"),
                Priority = ReadInt(item, "priority"),
                YearEstablished = ReadInt(item, "yearEstablished", "year_established"),
                Rating = ReadDouble(item, "rating"),
                ImageUrl = ReadString(item, "imageUrl", "image_url", "cloudinaryUrl", "cloudinary_url"),
                ImageFileName = ReadString(item, "imageFileName", "image_file_name"),
                IsActive = ReadBool(item, "isActive", "is_active", defaultValue: true)
            };

            if (!string.IsNullOrWhiteSpace(poi.Name))
            {
                result.Add(poi);
            }
        }

        return result;
    }

    private static List<PoiMenuItem> ParseMenuItems(JsonElement? element)
    {
        var result = new List<PoiMenuItem>();
        if (!element.HasValue)
        {
            return result;
        }

        foreach (var (item, key) in EnumerateCollection(element.Value))
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var menuItem = new PoiMenuItem
            {
                Id = ReadInt(item, "id", key),
                PoiId = ReadInt(item, "poiId", "poi_id"),
                Name = ReadString(item, "name"),
                Description = ReadString(item, "description"),
                Price = ReadDecimal(item, "price"),
                IsSignature = ReadBool(item, "isSignature", "is_signature")
            };

            if (!string.IsNullOrWhiteSpace(menuItem.Name))
            {
                result.Add(menuItem);
            }
        }

        return result;
    }

    private static IEnumerable<(JsonElement Value, string Key)> EnumerateCollection(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                yield return (item, index.ToString(CultureInfo.InvariantCulture));
                index++;
            }

            yield break;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                yield return (property.Value, property.Name);
            }
        }
    }

    private static string ReadString(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = TryGet(obj, name);
            if (!prop.HasValue)
            {
                continue;
            }

            var value = prop.Value;
            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }

            if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString();
            }
        }

        return string.Empty;
    }

    private static int ReadInt(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = TryGet(obj, name);
            if (!prop.HasValue)
            {
                continue;
            }

            var value = prop.Value;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
            {
                return number;
            }
        }

        return 0;
    }

    private static double ReadDouble(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = TryGet(obj, name);
            if (!prop.HasValue)
            {
                continue;
            }

            var value = prop.Value;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return 0;
    }

    private static decimal ReadDecimal(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            var prop = TryGet(obj, name);
            if (!prop.HasValue)
            {
                continue;
            }

            var value = prop.Value;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return 0;
    }

    private static bool ReadBool(JsonElement obj, string name1, string? name2 = null, bool defaultValue = false)
    {
        var names = name2 is null ? new[] { name1 } : new[] { name1, name2 };
        foreach (var name in names)
        {
            var prop = TryGet(obj, name);
            if (!prop.HasValue)
            {
                continue;
            }

            var value = prop.Value;
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
            {
                return n != 0;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var s = value.GetString();
                if (bool.TryParse(s, out var b))
                {
                    return b;
                }

                if (int.TryParse(s, out n))
                {
                    return n != 0;
                }
            }
        }

        return defaultValue;
    }

    private sealed record ParsedData(
        List<Category> Categories,
        List<Poi> Pois,
        List<PoiMenuItem> MenuItems);
}
