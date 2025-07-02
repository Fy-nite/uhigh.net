using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace uhigh.StdLib
{
    /// <summary>
    /// In-memory cache with expiration and LRU eviction
    /// </summary>
    public class Cache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache = new();
        private readonly int _maxSize;
        private readonly TimeSpan _defaultExpiration;
        private readonly object _lock = new object();

        public Cache(int maxSize = 1000, TimeSpan? defaultExpiration = null)
        {
            _maxSize = maxSize;
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
        }

        /// <summary>
        /// Get value from cache
        /// </summary>
        public TValue? Get(TKey key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    return default;
                }
                
                item.LastAccessed = DateTime.UtcNow;
                return item.Value;
            }
            
            return default;
        }

        /// <summary>
        /// Set value in cache
        /// </summary>
        public void Set(TKey key, TValue value, TimeSpan? expiration = null)
        {
            var item = new CacheItem<TValue>
            {
                Value = value,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration ?? _defaultExpiration)
            };

            _cache[key] = item;
            
            // Check if we need to evict items
            if (_cache.Count > _maxSize)
            {
                EvictLeastRecentlyUsed();
            }
        }

        /// <summary>
        /// Get or set value using factory function
        /// </summary>
        public TValue GetOrSet(TKey key, Func<TValue> factory, TimeSpan? expiration = null)
        {
            var value = Get(key);
            if (value == null || value.Equals(default(TValue)))
            {
                value = factory();
                Set(key, value, expiration);
            }
            return value;
        }

        /// <summary>
        /// Remove value from cache
        /// </summary>
        public bool Remove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return _cache.ContainsKey(key) && !_cache[key].IsExpired;
        }

        /// <summary>
        /// Clear all cache entries
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetStats()
        {
            var items = _cache.Values.ToList();
            var expired = items.Count(i => i.IsExpired);
            
            return new CacheStats
            {
                TotalItems = items.Count,
                ExpiredItems = expired,
                ActiveItems = items.Count - expired,
                MemoryUsage = EstimateMemoryUsage()
            };
        }

        /// <summary>
        /// Clean up expired items
        /// </summary>
        public void Cleanup()
        {
            var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        private void EvictLeastRecentlyUsed()
        {
            lock (_lock)
            {
                var itemsToEvict = _cache.Count - _maxSize + 1;
                var oldestItems = _cache.OrderBy(kvp => kvp.Value.LastAccessed).Take(itemsToEvict);
                
                foreach (var item in oldestItems)
                {
                    _cache.TryRemove(item.Key, out _);
                }
            }
        }

        private long EstimateMemoryUsage()
        {
            // Rough estimation - in practice you'd want more sophisticated calculation
            return _cache.Count * 100; // Assume 100 bytes per item
        }
    }

    /// <summary>
    /// Cache item wrapper
    /// </summary>
    public class CacheItem<T>
    {
        public T Value { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStats
    {
        public int TotalItems { get; set; }
        public int ExpiredItems { get; set; }
        public int ActiveItems { get; set; }
        public long MemoryUsage { get; set; }
    }

    /// <summary>
    /// Static cache for global use
    /// </summary>
    public static class GlobalCache
    {
        private static readonly Cache<string, object> _cache = new Cache<string, object>();

        public static T? Get<T>(string key) => (T?)_cache.Get(key);
        public static void Set<T>(string key, T value, TimeSpan? expiration = null) => _cache.Set(key, value!, expiration);
        public static T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null) => (T)_cache.GetOrSet(key, () => factory()!, expiration);
        public static bool Remove(string key) => _cache.Remove(key);
        public static void Clear() => _cache.Clear();
        public static CacheStats GetStats() => _cache.GetStats();
    }
}
