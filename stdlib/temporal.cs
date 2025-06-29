using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.Json;

namespace uhigh.StdLib
{
    /// <summary>
    /// Represents a snapshot of an object at a specific point in time
    /// </summary>
    public class Snapshot<T>
    {
        public DateTime Timestamp { get; set; }
        public T Value { get; set; }
        public string? ChangeReason { get; set; }
        
        public Snapshot(T value, string? reason = null)
        {
            Value = value;
            Timestamp = DateTime.UtcNow;
            ChangeReason = reason;
        }
    }

    /// <summary>
    /// Temporal container that tracks changes to an object over time
    /// </summary>
    public class Temporal<T> where T : class
    {
        private readonly ConcurrentQueue<Snapshot<T>> _snapshots = new();
        private readonly int _maxHistory;
        private T _current;

        public Temporal(T initialValue, int maxHistory = 100)
        {
            _current = initialValue;
            _maxHistory = maxHistory;
            _snapshots.Enqueue(new Snapshot<T>(DeepClone(initialValue), "initial"));
        }

        /// <summary>
        /// Gets the current value
        /// </summary>
        public T Current => _current;

        /// <summary>
        /// Updates the value and creates a snapshot
        /// </summary>
        public void Update(T newValue, string? reason = null)
        {
            _current = newValue;
            _snapshots.Enqueue(new Snapshot<T>(DeepClone(newValue), reason));
            
            // Maintain history limit
            while (_snapshots.Count > _maxHistory)
            {
                _snapshots.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Get the value as it was X seconds ago
        /// </summary>
        public T? GetSecondsAgo(double seconds)
        {
            var targetTime = DateTime.UtcNow.AddSeconds(-seconds);
            return GetValueAt(targetTime);
        }

        /// <summary>
        /// Get the value as it was X minutes ago
        /// </summary>
        public T? GetMinutesAgo(double minutes)
        {
            var targetTime = DateTime.UtcNow.AddMinutes(-minutes);
            return GetValueAt(targetTime);
        }

        /// <summary>
        /// Get the value at a specific timestamp
        /// </summary>
        public T? GetValueAt(DateTime timestamp)
        {
            Snapshot<T>? bestMatch = null;
            
            foreach (var snapshot in _snapshots)
            {
                if (snapshot.Timestamp <= timestamp)
                {
                    if (bestMatch == null || snapshot.Timestamp > bestMatch.Timestamp)
                    {
                        bestMatch = snapshot;
                    }
                }
            }
            
            return bestMatch?.Value;
        }

        /// <summary>
        /// Get all changes within a time range
        /// </summary>
        public IEnumerable<Snapshot<T>> GetChangesInRange(DateTime start, DateTime end)
        {
            return _snapshots.Where(s => s.Timestamp >= start && s.Timestamp <= end);
        }

        /// <summary>
        /// Get the history of changes with reasons
        /// </summary>
        public IEnumerable<Snapshot<T>> GetHistory()
        {
            return _snapshots.ToArray().Reverse();
        }

        /// <summary>
        /// Rollback to a previous state
        /// </summary>
        public bool RollbackTo(DateTime timestamp)
        {
            var targetSnapshot = GetValueAt(timestamp);
            if (targetSnapshot != null)
            {
                Update(targetSnapshot, $"rollback to {timestamp}");
                return true;
            }
            return false;
        }

        private T DeepClone(T obj)
        {
            // Simple deep clone using JSON serialization
            // In production, you might want a more efficient method
            var json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }

    /// <summary>
    /// Extension methods for easy temporal operations
    /// </summary>
    public static class TemporalExtensions
    {
        /// <summary>
        /// Wrap any object in a temporal container
        /// </summary>
        public static Temporal<T> ToTemporal<T>(this T obj, int maxHistory = 100) where T : class
        {
            return new Temporal<T>(obj, maxHistory);
        }

        /// <summary>
        /// Create a temporal array with snapshot capabilities
        /// </summary>
        public static Temporal<List<T>> ToTemporalArray<T>(this List<T> list, int maxHistory = 50)
        {
            return new Temporal<List<T>>(new List<T>(list), maxHistory);
        }
    }

    /// <summary>
    /// Temporal dictionary that tracks changes to key-value pairs
    /// </summary>
    public class TemporalDictionary<TKey, TValue> where TKey : notnull where TValue : class
    {
        private readonly Dictionary<TKey, Temporal<TValue>> _temporalValues = new();

        public void Set(TKey key, TValue value, string? reason = null)
        {
            if (!_temporalValues.ContainsKey(key))
            {
                _temporalValues[key] = new Temporal<TValue>(value, 50);
            }
            else
            {
                _temporalValues[key].Update(value, reason);
            }
        }

        public TValue? Get(TKey key)
        {
            return _temporalValues.ContainsKey(key) ? _temporalValues[key].Current : default;
        }

        public TValue? GetSecondsAgo(TKey key, double seconds)
        {
            return _temporalValues.ContainsKey(key) ? _temporalValues[key].GetSecondsAgo(seconds) : default;
        }

        public TValue? GetMinutesAgo(TKey key, double minutes)
        {
            return _temporalValues.ContainsKey(key) ? _temporalValues[key].GetMinutesAgo(minutes) : default;
        }

        public IEnumerable<TKey> Keys => _temporalValues.Keys;
    }
}
