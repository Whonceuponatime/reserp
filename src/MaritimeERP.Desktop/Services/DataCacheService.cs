using System.Collections.Concurrent;

namespace MaritimeERP.Desktop.Services
{
    public interface IDataCacheService
    {
        T? Get<T>(string key) where T : class;
        void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        void Remove(string key);
        void Clear();
        bool Exists(string key);
    }

    public class DataCacheService : IDataCacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly Timer _cleanupTimer;

        public DataCacheService()
        {
            // Clean up expired items every 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public T? Get<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt == null || item.ExpiresAt > DateTime.UtcNow)
                {
                    return item.Value as T;
                }
                else
                {
                    // Item expired, remove it
                    _cache.TryRemove(key, out _);
                }
            }
            return null;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;
            var item = new CacheItem(value, expiresAt);
            _cache.AddOrUpdate(key, item, (k, v) => item);
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public bool Exists(string key)
        {
            return _cache.ContainsKey(key) && Get<object>(key) != null;
        }

        private void CleanupExpiredItems(object? state)
        {
            var expiredKeys = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt <= now)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        private class CacheItem
        {
            public object Value { get; }
            public DateTime? ExpiresAt { get; }

            public CacheItem(object value, DateTime? expiresAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
            }
        }
    }
} 