using SQLite;
using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;
    private bool _initialized;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private const string SeedDbVersionKey = "SeedDbVersion";
    private const int CurrentSeedDbVersion = 7;  // ← Tăng lên (đã làm rồi)

    private static string DbPath =>
        Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");

    public async Task InitAsync()
    {
        if (_initialized) return;

        var savedVersion = Preferences.Get(SeedDbVersionKey, 0);
        // Chỉ copy lại DB nếu file không tồn tại HOẶC version cũ hơn
        var needCopySeedDb = !File.Exists(DbPath) || savedVersion < CurrentSeedDbVersion;

        if (needCopySeedDb)
        {
            if (File.Exists(DbPath))
                File.Delete(DbPath);  // ← Xóa DB cũ

            // Copy DB từ Assets
            await using var stream = await FileSystem.OpenAppPackageFileAsync("vinhkhanh.db");
            await using var dest = File.Create(DbPath);
            await stream.CopyToAsync(dest);

            Preferences.Set(SeedDbVersionKey, CurrentSeedDbVersion);
        }

        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);

        await EnsurePoiSchemaAsync();  // ← Thêm 2 cột nếu chưa có

        _initialized = true;
    }

    private async Task EnsurePoiSchemaAsync()
    {
        if (_db is null) return;

        if (!await ColumnExistsAsync("Pois", "HistoryEn"))
        {
            await _db.ExecuteAsync("ALTER TABLE Pois ADD COLUMN HistoryEn TEXT NOT NULL DEFAULT '';" );
        }

        if (!await ColumnExistsAsync("Pois", "AdrEn"))
        {
            await _db.ExecuteAsync("ALTER TABLE Pois ADD COLUMN AdrEn TEXT NOT NULL DEFAULT '';" );
        }
    }

    private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        if (_db is null) return false;

        var rows = await _db.QueryAsync<PragmaTableInfoRow>($"PRAGMA table_info([{tableName}]);");
        return rows.Any(r => string.Equals(r.name, columnName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class PragmaTableInfoRow
    {
        public string name { get; set; } = string.Empty;
    }

    private void EnsureInit()
    {
        if (!_initialized || _db is null)
            throw new InvalidOperationException("Gọi InitAsync() trước.");
    }

    public async Task<List<Poi>> GetPoisMissingEnglishFieldsAsync()
    {
        EnsureInit();
        return await _db!.QueryAsync<Poi>(
            "SELECT * FROM Pois WHERE IFNULL(HistoryEn,'') = '' OR IFNULL(AdrEn,'') = ''");
    }

    public async Task UpdatePoiEnglishFieldsAsync(int poiId, string historyEn, string adrEn)
    {
        EnsureInit();
        await _db!.ExecuteAsync(
            "UPDATE Pois SET HistoryEn = ?, AdrEn = ? WHERE Id = ?",
            historyEn ?? string.Empty,
            adrEn ?? string.Empty,
            poiId);
    }

    public async Task ReplaceSeedDataAsync(
        IEnumerable<Category> categories,
        IEnumerable<Poi> pois,
        IEnumerable<PoiMenuItem> menuItems)
    {
        EnsureInit();

        var categoryList = categories?.ToList() ?? new List<Category>();
        var poiList = pois?.ToList() ?? new List<Poi>();
        var menuItemList = menuItems?.ToList() ?? new List<PoiMenuItem>();

        // Bảo vệ dữ liệu local: chỉ ghi đè khi có dữ liệu POI hợp lệ từ nguồn sync
        if (poiList.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[DatabaseService] Skip ReplaceSeedDataAsync because POI list is empty.");
            return;
        }

        await _syncLock.WaitAsync();
        try
        {
            await _db!.RunInTransactionAsync(conn =>
            {
                conn.DeleteAll<PoiMenuItem>();
                conn.DeleteAll<Poi>();
                conn.DeleteAll<Category>();

                foreach (var c in categoryList)
                {
                    conn.InsertOrReplace(c);
                }

                foreach (var p in poiList)
                {
                    conn.InsertOrReplace(p);
                }

                foreach (var m in menuItemList)
                {
                    conn.InsertOrReplace(m);
                }
            });
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        EnsureInit();
        return await _db!.Table<Category>()
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task<List<Poi>> GetAllPoisAsync()
    {
        EnsureInit();

        var pois = await _db!.Table<Poi>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync();

        var catDict = (await GetCategoriesAsync())
            .ToDictionary(c => c.Id, c => c.Name);

        foreach (var poi in pois)
        {
            poi.CategoryName = catDict.TryGetValue(poi.CategoryId, out var name)
                ? name
                : string.Empty;
        }

        return pois;
    }

    public async Task<Poi?> GetPoiByIdAsync(int id)
    {
        EnsureInit();

        var poi = await _db!.Table<Poi>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi is null) return null;

        var cat = await _db.Table<Category>()
            .FirstOrDefaultAsync(c => c.Id == poi.CategoryId);

        poi.CategoryName = cat?.Name ?? string.Empty;
        return poi;
    }

    public async Task<List<PoiMenuItem>> GetMenuItemsAsync(int poiId)
    {
        EnsureInit();
        return await _db!.Table<PoiMenuItem>()
            .Where(m => m.PoiId == poiId)
            .ToListAsync();
    }

    public async Task<PlaybackLog?> GetLastLogAsync(int poiId, string deviceId)
    {
        EnsureInit();
        return await _db!.Table<PlaybackLog>()
            .Where(l => l.PoiId == poiId && l.DeviceId == deviceId)
            .OrderByDescending(l => l.PlayedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddLogAsync(PlaybackLog log)
    {
        EnsureInit();
        await _db!.InsertAsync(log);
    }

    public async Task CleanOldLogsAsync()
    {
        EnsureInit();
        var cutoff = DateTime.Now.AddDays(-7);
        await _db!.Table<PlaybackLog>()
            .DeleteAsync(l => l.PlayedAt < cutoff);
    }

    public async Task<TranslationCache?> GetTranslationCacheAsync(int poiId, string language)
    {
        EnsureInit();
        return await _db!.Table<TranslationCache>()
            .FirstOrDefaultAsync(t => t.PoiId == poiId && t.Language == language);
    }

    public async Task SaveTranslationCacheAsync(TranslationCache cache)
    {
        EnsureInit();

        var existing = await GetTranslationCacheAsync(cache.PoiId, cache.Language);
        if (existing is not null)
        {
            cache.Id = existing.Id;
            await _db!.UpdateAsync(cache);
        }
        else
        {
            await _db!.InsertAsync(cache);
        }
    }

    public async Task CleanExpiredCacheAsync()
    {
        EnsureInit();
        var cutoff = DateTime.Now.AddDays(-30);
        await _db!.Table<TranslationCache>()
            .DeleteAsync(t => t.CachedAt < cutoff);
    }

    public async Task<bool> HasAnyPoiAsync()
    {
        EnsureInit();
        var count = await _db!.Table<Poi>().CountAsync();
        return count > 0;
    }
}