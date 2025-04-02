using System.Text.Json;
using StackExchange.Redis;

namespace API.Services
{
    public class RedisService
    {
        private readonly IDatabase _database;
        private readonly JsonSerializerOptions _jsonOptions;
        private const int DEFAULT_EXPIRY_MINUTES = 60;

        public RedisService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
            _jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        #region String Operations
        public async Task<string?> GetStringAsync(string key)
        {
            return await _database.StringGetAsync(key);
        }

        public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            return await _database.StringSetAsync(key, value, expiry ?? TimeSpan.FromMinutes(DEFAULT_EXPIRY_MINUTES));
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            return await _database.StringIncrementAsync(key, value);
        }

        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            return await _database.StringDecrementAsync(key, value);
        }
        #endregion

        #region Object Operations
        public async Task<T?> GetObjectAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.StringSetAsync(key, jsonData, expiry ?? TimeSpan.FromMinutes(DEFAULT_EXPIRY_MINUTES));
        }
        #endregion

        #region List Operations
        public async Task<List<T>?> GetListAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            return value.IsNull ? new List<T>() : JsonSerializer.Deserialize<List<T>>(value!, _jsonOptions);
        }

        public async Task<bool> SetListAsync<T>(string key, List<T> value, TimeSpan? expiry = null)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.StringSetAsync(key, jsonData, expiry ?? TimeSpan.FromMinutes(DEFAULT_EXPIRY_MINUTES));
        }

        public async Task<long> ListRightPushAsync<T>(string key, T value)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.ListRightPushAsync(key, jsonData);
        }

        public async Task<T?> ListLeftPopAsync<T>(string key)
        {
            var value = await _database.ListLeftPopAsync(key);
            return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }

        public async Task<long> GetListLengthAsync(string key)
        {
            return await _database.ListLengthAsync(key);
        }
        #endregion

        #region Hash Operations
        public async Task<bool> HashSetAsync<T>(string key, string hashField, T value)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.HashSetAsync(key, hashField, jsonData);
        }

        public async Task<T?> HashGetAsync<T>(string key, string hashField)
        {
            var value = await _database.HashGetAsync(key, hashField);
            return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }

        public async Task<Dictionary<string, T>> HashGetAllAsync<T>(string key)
        {
            var entries = await _database.HashGetAllAsync(key);
            return entries.ToDictionary(
                entry => entry.Name.ToString(),
                entry => JsonSerializer.Deserialize<T>(entry.Value!, _jsonOptions)!
            );
        }

        public async Task<bool> HashDeleteAsync(string key, string hashField)
        {
            return await _database.HashDeleteAsync(key, hashField);
        }
        #endregion

        #region Set Operations
        public async Task<bool> SetAddAsync<T>(string key, T value)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.SetAddAsync(key, jsonData);
        }

        public async Task<bool> SetRemoveAsync<T>(string key, T value)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.SetRemoveAsync(key, jsonData);
        }

        public async Task<bool> SetContainsAsync<T>(string key, T value)
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.SetContainsAsync(key, jsonData);
        }

        public async Task<List<T>> SetMembersAsync<T>(string key)
        {
            var members = await _database.SetMembersAsync(key);
            return members.Select(m => JsonSerializer.Deserialize<T>(m!, _jsonOptions)!).ToList();
        }
        #endregion

        #region Key Operations
        public async Task<bool> KeyExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        public async Task<bool> KeyDeleteAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> KeyExpireAsync(string key, TimeSpan? expiry)
        {
            return await _database.KeyExpireAsync(key, expiry);
        }

        public async Task<TimeSpan?> KeyTimeToLiveAsync(string key)
        {
            return await _database.KeyTimeToLiveAsync(key);
        }
        #endregion

        #region Transaction Operations
        public ITransaction CreateTransaction()
        {
            return _database.CreateTransaction();
        }
        #endregion

        #region Batch Operations
        public IBatch CreateBatch()
        {
            return _database.CreateBatch();
        }
        #endregion
    }
}