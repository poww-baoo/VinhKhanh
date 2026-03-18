using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using VinhKhanh.Models;

namespace VinhKhanh
{
    public class SQLiteDbContext
    {
        private SQLiteAsyncConnection? _database;
        private static readonly string DbPath = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");

        public SQLiteAsyncConnection Database
        {
            get
            {
                _database ??= new SQLiteAsyncConnection(DbPath);
                return _database;
            }
        }

        /// <summary>
        /// Khởi tạo database và tạo các table
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await Database.CreateTableAsync<RestaurantEntity>();
                await Database.CreateTableAsync<CategoryEntity>();
                await Database.CreateTableAsync<SavedRestaurantEntity>();
                
                Debug.WriteLine($"Database initialized at: {DbPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        // ====== RESTAURANT OPERATIONS ======
        public async Task<List<RestaurantEntity>> GetAllRestaurantsAsync()
        {
            try
            {
                return await Database.Table<RestaurantEntity>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching restaurants: {ex.Message}");
                return new List<RestaurantEntity>();
            }
        }

        public async Task<RestaurantEntity?> GetRestaurantByIdAsync(string id)
        {
            try
            {
                return await Database.Table<RestaurantEntity>()
                    .Where(r => r.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching restaurant: {ex.Message}");
                return null;
            }
        }

        public async Task<int> InsertRestaurantAsync(RestaurantEntity restaurant)
        {
            try
            {
                return await Database.InsertAsync(restaurant);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting restaurant: {ex.Message}");
                return -1;
            }
        }

        public async Task<int> UpdateRestaurantAsync(RestaurantEntity restaurant)
        {
            try
            {
                return await Database.UpdateAsync(restaurant);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating restaurant: {ex.Message}");
                return -1;
            }
        }

        public async Task<int> DeleteRestaurantAsync(string id)
        {
            try
            {
                var restaurant = await GetRestaurantByIdAsync(id);
                if (restaurant != null)
                {
                    return await Database.DeleteAsync(restaurant);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting restaurant: {ex.Message}");
                return -1;
            }
        }

        // ====== CATEGORY OPERATIONS ======
        public async Task<List<CategoryEntity>> GetAllCategoriesAsync()
        {
            try
            {
                return await Database.Table<CategoryEntity>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching categories: {ex.Message}");
                return new List<CategoryEntity>();
            }
        }

        public async Task<int> InsertCategoryAsync(CategoryEntity category)
        {
            try
            {
                return await Database.InsertAsync(category);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting category: {ex.Message}");
                return -1;
            }
        }

        // ====== SAVED RESTAURANT OPERATIONS ======
        public async Task<List<SavedRestaurantEntity>> GetAllSavedRestaurantsAsync()
        {
            try
            {
                return await Database.Table<SavedRestaurantEntity>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching saved restaurants: {ex.Message}");
                return new List<SavedRestaurantEntity>();
            }
        }

        public async Task<bool> IsSavedAsync(string restaurantId)
        {
            try
            {
                var saved = await Database.Table<SavedRestaurantEntity>()
                    .Where(s => s.RestaurantId == restaurantId)
                    .FirstOrDefaultAsync();
                return saved != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking saved status: {ex.Message}");
                return false;
            }
        }

        public async Task<int> SaveRestaurantAsync(string restaurantId)
        {
            try
            {
                var saved = new SavedRestaurantEntity
                {
                    RestaurantId = restaurantId,
                    SavedAt = DateTime.Now
                };
                return await Database.InsertAsync(saved);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving restaurant: {ex.Message}");
                return -1;
            }
        }

        public async Task<int> RemoveSavedRestaurantAsync(string restaurantId)
        {
            try
            {
                return await Database.ExecuteAsync(
                    "DELETE FROM SavedRestaurantEntity WHERE RestaurantId = ?",
                    restaurantId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing saved restaurant: {ex.Message}");
                return -1;
            }
        }

        // ====== DATABASE MAINTENANCE ======
        public async Task ClearAllDataAsync()
        {
            try
            {
                await Database.DeleteAllAsync<RestaurantEntity>();
                await Database.DeleteAllAsync<CategoryEntity>();
                await Database.DeleteAllAsync<SavedRestaurantEntity>();
                Debug.WriteLine("All data cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing data: {ex.Message}");
            }
        }

        public async Task CloseAsync()
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }
        }
    }

    // ====== DATABASE ENTITIES ======
    [Table("Restaurants")]
    public class RestaurantEntity
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Name { get; set; }
        public int YearEstablished { get; set; }
        public string History { get; set; }
        public string Highlights { get; set; }
        public double Rating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GeofenceRadius { get; set; } = 100;
        public int Priority { get; set; }
    }

    [Table("Categories")]
    public class CategoryEntity
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Name { get; set; }
    }

    [Table("SavedRestaurants")]
    public class SavedRestaurantEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string RestaurantId { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
