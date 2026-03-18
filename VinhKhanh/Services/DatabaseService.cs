using SQLite;
using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;
    private bool _initialized;

    private static string DbPath =>
        Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");

    public async Task InitAsync()
    {
        if (_initialized) return;

        if (!File.Exists(DbPath))
        {
            await using var stream =
                await FileSystem.OpenAppPackageFileAsync("vinhkhanh.db");
            await using var dest = File.Create(DbPath);
            await stream.CopyToAsync(dest);
        }

        _db = new SQLiteAsyncConnection(DbPath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<PlaybackLog>();
        await _db.CreateTableAsync<TranslationCache>();

        _initialized = true;
    }

    private void EnsureInit()
    {
        if (!_initialized || _db is null)
            throw new InvalidOperationException("Gọi InitAsync() trước.");
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
}