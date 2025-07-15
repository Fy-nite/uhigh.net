namespace StdLib
{
    /// <summary>
    /// Advanced collection utilities beyond basic arrays
    /// </summary>
    public static class Collections
    {
        /// <summary>
        /// Create a range of numbers
        /// </summary>
        public static List<int> Range(int start, int end, int step = 1)
        {
            var result = new List<int>();
            if (step > 0)
            {
                for (int i = start; i < end; i += step)
                    result.Add(i);
            }
            else if (step < 0)
            {
                for (int i = start; i > end; i += step)
                    result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// Chunk an array into smaller arrays of specified size
        /// </summary>
        public static List<List<T>> Chunk<T>(this List<T> source, int size)
        {
            var result = new List<List<T>>();
            for (int i = 0; i < source.Count; i += size)
            {
                result.Add(source.Skip(i).Take(size).ToList());
            }
            return result;
        }

        /// <summary>
        /// Flatten nested arrays
        /// </summary>
        public static List<T> Flatten<T>(this List<List<T>> nested)
        {
            return nested.SelectMany(x => x).ToList();
        }

        /// <summary>
        /// Find differences between two arrays
        /// </summary>
        public static ArrayDiff<T> Diff<T>(this List<T> original, List<T> modified)
        {
            var added = modified.Except(original).ToList();
            var removed = original.Except(modified).ToList();
            var common = original.Intersect(modified).ToList();

            return new ArrayDiff<T>
            {
                Added = added,
                Removed = removed,
                Common = common,
                HasChanges = added.Count > 0 || removed.Count > 0
            };
        }

        /// <summary>
        /// Group consecutive elements
        /// </summary>
        public static List<List<T>> GroupConsecutive<T>(this List<T> source, Func<T, T, bool> areConsecutive)
        {
            var groups = new List<List<T>>();
            if (!source.Any()) return groups;

            var currentGroup = new List<T> { source.First() };

            for (int i = 1; i < source.Count; i++)
            {
                if (areConsecutive(source[i - 1], source[i]))
                {
                    currentGroup.Add(source[i]);
                }
                else
                {
                    groups.Add(currentGroup);
                    currentGroup = new List<T> { source[i] };
                }
            }
            groups.Add(currentGroup);

            return groups;
        }

        /// <summary>
        /// Rotate array elements
        /// </summary>
        public static List<T> Rotate<T>(this List<T> source, int positions)
        {
            if (source.Count == 0) return source;

            positions = positions % source.Count;
            if (positions < 0) positions += source.Count;

            return source.Skip(positions).Concat(source.Take(positions)).ToList();
        }

        /// <summary>
        /// Find the most frequent element
        /// </summary>
        public static T? MostFrequent<T>(this List<T> source)
        {
            return source.GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()!.Key;
        }

        /// <summary>
        /// Sliding window over array
        /// </summary>
        public static IEnumerable<List<T>> SlidingWindow<T>(this List<T> source, int windowSize)
        {
            for (int i = 0; i <= source.Count - windowSize; i++)
            {
                yield return source.Skip(i).Take(windowSize).ToList();
            }
        }
    }

    /// <summary>
    /// The array diff class
    /// </summary>
    public class ArrayDiff<T>
    {
        /// <summary>
        /// Gets or sets the value of the added
        /// </summary>
        public List<T> Added { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the removed
        /// </summary>
        public List<T> Removed { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the common
        /// </summary>
        public List<T> Common { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the has changes
        /// </summary>
        public bool HasChanges { get; set; }
    }

    /// <summary>
    /// Thread-safe collection with temporal tracking
    /// </summary>
    public class TemporalConcurrentList<T> where T : class
    {
        /// <summary>
        /// The lock
        /// </summary>
        private readonly object _lock = new object();
        /// <summary>
        /// The temporal
        /// </summary>
        private readonly Temporal<List<T>> _temporal;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporalConcurrentList{T}"/> class
        /// </summary>
        public TemporalConcurrentList()
        {
            _temporal = new Temporal<List<T>>(new List<T>());
        }

        /// <summary>
        /// Adds the item
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="reason">The reason</param>
        public void Add(T item, string? reason = null)
        {
            lock (_lock)
            {
                var newList = new List<T>(_temporal.Current) { item };
                _temporal.Update(newList, reason ?? $"added {item}");
            }
        }

        /// <summary>
        /// Removes the item
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="reason">The reason</param>
        public void Remove(T item, string? reason = null)
        {
            lock (_lock)
            {
                var newList = new List<T>(_temporal.Current);
                newList.Remove(item);
                _temporal.Update(newList, reason ?? $"removed {item}");
            }
        }

        /// <summary>
        /// Gets the snapshot
        /// </summary>
        /// <returns>A list of t</returns>
        public List<T> GetSnapshot() => new List<T>(_temporal.Current);
        /// <summary>
        /// Gets the seconds ago using the specified seconds
        /// </summary>
        /// <param name="seconds">The seconds</param>
        /// <returns>A list of t</returns>
        public List<T>? GetSecondsAgo(double seconds) => _temporal.GetSecondsAgo(seconds);
        /// <summary>
        /// Gets the minutes ago using the specified minutes
        /// </summary>
        /// <param name="minutes">The minutes</param>
        /// <returns>A list of t</returns>
        public List<T>? GetMinutesAgo(double minutes) => _temporal.GetMinutesAgo(minutes);
    }
}
