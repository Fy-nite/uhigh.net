using System;
using System.Collections.Generic;
using System.Linq;

namespace uhigh.StdLib
{

    [System.Serializable]
    public class Array<T>
    {
        private List<T> _items;

        public Array()
        {
            _items = new List<T>();
        }

        public Array(IEnumerable<T> items)
        {
            _items = new List<T>(items);
        }

        public int Count => _items.Count;

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public IEnumerable<T> GetItems()
        {
            return _items.AsEnumerable();
        }
    }


    /// <summary>
    /// Represents a slice/view of an array starting from a specific index
    /// </summary>
    [System.Serializable]
    public class ArrayIndice<T> : IEnumerable<T>, ICollection<T>
    {
        private readonly List<T> _sourceArray;
        private readonly int _startOffset;
        private readonly List<T> _localItems;

        public ArrayIndice(List<T> sourceArray, int startOffset)
        {
            _sourceArray = sourceArray ?? throw new ArgumentNullException(nameof(sourceArray));
            _startOffset = startOffset;
            _localItems = new List<T>();
        }

        /// <summary>
        /// Gets the starting offset in the original array
        /// </summary>
        public int StartOffset => _startOffset;

        /// <summary>
        /// Gets the count of items in this indice
        /// </summary>
        public int Count => _localItems.Count;

        /// <summary>
        /// Access item at local index (0-based within this indice)
        /// </summary>
        public T this[int localIndex]
        {
            get => _localItems[localIndex];
            set => _localItems[localIndex] = value;
        }

        /// <summary>
        /// Collect item from source array at the specified global index
        /// </summary>
        public ArrayIndice<T> Collect(int globalIndex)
        {
            if (globalIndex < _startOffset)
                throw new ArgumentException($"Index {globalIndex} is before start offset {_startOffset}");
            
            if (globalIndex < _sourceArray.Count)
            {
                var localIndex = globalIndex - _startOffset;
                while (_localItems.Count <= localIndex)
                {
                    _localItems.Add(default(T));
                }
                _localItems[localIndex] = _sourceArray[globalIndex];
            }
            return this;
        }

        /// <summary>
        /// Get item at global index (relative to original array)
        /// </summary>
        public T At(int globalIndex)
        {
            var localIndex = globalIndex - _startOffset;
            if (localIndex >= 0 && localIndex < _localItems.Count)
                return _localItems[localIndex];
            
            if (globalIndex < _sourceArray.Count)
                return _sourceArray[globalIndex];
            
            throw new IndexOutOfRangeException($"Index {globalIndex} is out of range");
        }

        /// <summary>
        /// Convert this indice to a regular array starting from index 0
        /// </summary>
        public List<T> MapToArray()
        {
            return new List<T>(_localItems);
        }

        /// <summary>
        /// Add item to this indice
        /// </summary>
        public void Add(T item)
        {
            _localItems.Add(item);
        }

        /// <summary>
        /// Collect all items from the source array starting from the offset
        /// </summary>
        public ArrayIndice<T> CollectAll()
        {
            for (int i = _startOffset; i < _sourceArray.Count; i++)
            {
                Collect(i);
            }
            return this;
        }

        /// <summary>
        /// Return all items in this indice back to the source array
        /// </summary>
        public List<T> Return(List<T> targetArray)
        {
            // Ensure target array is large enough
            var requiredSize = _startOffset + _localItems.Count;
            while (targetArray.Count < requiredSize)
            {
                targetArray.Add(default(T));
            }

            // Copy items back to their global positions
            for (int i = 0; i < _localItems.Count; i++)
            {
                var globalIndex = _startOffset + i;
                if (globalIndex < targetArray.Count)
                {
                    targetArray[globalIndex] = _localItems[i];
                }
                else
                {
                    targetArray.Add(_localItems[i]);
                }
            }

            return targetArray;
        }

        /// <summary>
        /// Get all items as enumerable
        /// </summary>
        public IEnumerable<T> GetItems()
        {
            return _localItems;
        }

        /// <summary>
        /// Gets the type of elements in this indice for reflection
        /// </summary>
        public Type ElementType => typeof(T);

        /// <summary>
        /// Gets whether this indice is empty
        /// </summary>
        public bool IsEmpty => _localItems.Count == 0;

        /// <summary>
        /// Gets whether this indice is read-only
        /// </summary>
        public bool IsReadOnly => false;

        #region IEnumerable<T> Implementation
        public IEnumerator<T> GetEnumerator()
        {
            return _localItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ICollection<T> Implementation
        public void Clear()
        {
            _localItems.Clear();
        }

        public bool Contains(T item)
        {
            return _localItems.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _localItems.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _localItems.Remove(item);
        }
        #endregion
    }

    /// <summary>
    /// Extension methods for array operations
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Create an array indice starting from the specified offset
        /// </summary>
        public static ArrayIndice<T> CreateIndice<T>(this List<T> array, int startOffset)
        {
            return new ArrayIndice<T>(array, startOffset);
        }

        /// <summary>
        /// Collect items from array into an indice
        /// </summary>
        public static ArrayIndice<T> Collect<T>(this List<T> array, ArrayIndice<T> indice)
        {
            return indice.CollectAll();
        }
    }
}
