using System;
using System.Collections.Generic;
using System.Linq;

namespace uhigh.StdLib
{

    /// <summary>
    /// The array class
    /// </summary>
    [System.Serializable]
    public class Array<T>
    {
        /// <summary>
        /// The items
        /// </summary>
        private List<T> _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="Array{T}"/> class
        /// </summary>
        public Array()
        {
            _items = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Array{T}"/> class
        /// </summary>
        /// <param name="items">The items</param>
        public Array(IEnumerable<T> items)
        {
            _items = new List<T>(items);
        }

        /// <summary>
        /// Gets the value of the count
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// The value
        /// </summary>
        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        /// <summary>
        /// Adds the item
        /// </summary>
        /// <param name="item">The item</param>
        public void Add(T item)
        {
            _items.Add(item);
        }

        /// <summary>
        /// Removes the at using the specified index
        /// </summary>
        /// <param name="index">The index</param>
        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        /// <summary>
        /// Clears this instance
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        /// Gets the items
        /// </summary>
        /// <returns>An enumerable of t</returns>
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
        /// <summary>
        /// The source array
        /// </summary>
        private readonly List<T> _sourceArray;
        /// <summary>
        /// The start offset
        /// </summary>
        private readonly int _startOffset;
        /// <summary>
        /// The local items
        /// </summary>
        private readonly List<T> _localItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayIndice{T}"/> class
        /// </summary>
        /// <param name="sourceArray">The source array</param>
        /// <param name="startOffset">The start offset</param>
        /// <exception cref="ArgumentNullException"></exception>
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
        /// <summary>
        /// Gets the enumerator
        /// </summary>
        /// <returns>An enumerator of t</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _localItems.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator
        /// </summary>
        /// <returns>The system collections enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ICollection<T> Implementation
        /// <summary>
        /// Clears this instance
        /// </summary>
        public void Clear()
        {
            _localItems.Clear();
        }

        /// <summary>
        /// Containses the item
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>The bool</returns>
        public bool Contains(T item)
        {
            return _localItems.Contains(item);
        }

        /// <summary>
        /// Copies the to using the specified array
        /// </summary>
        /// <param name="array">The array</param>
        /// <param name="arrayIndex">The array index</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _localItems.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the item
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>The bool</returns>
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
