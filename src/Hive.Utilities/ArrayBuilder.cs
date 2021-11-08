using System;
#if NETSTANDARD2_1
using System.Buffers;
#else
using System.Diagnostics.CodeAnalysis;
#endif

namespace Hive.Utilities
{
    /// <summary>
    /// A type that can be used to (fairly) efficiently create arrays with minimal allocation.
    /// </summary>
    /// <remarks>
    /// Before going out of scope, each instance <b>must</b> have either <see cref="ToArray"/> or <see cref="Clear"/> called.
    /// If used in a <see langword="using"/> scope, <see cref="Clear"/> is called automatically.
    /// </remarks>
    /// <typeparam name="T">The type of the array elements.</typeparam>
    public ref struct ArrayBuilder<T>
    {
        private T[] array;

#if NETSTANDARD2_1
        private ArrayPool<T> pool;
#endif

        /// <summary>
        /// The number of items currently in the <see cref="ArrayBuilder{T}"/>.
        /// </summary>
        public int Count { get; private set; }

#if !NETSTANDARD2_1
        [SuppressMessage("Style", "IDE0044:Add readonly modifier",
            Justification = "This is modified when using .NET Standard 2.1, and so is left as mutable for that.")]
#endif
        private bool rented;

        /// <summary>
        /// Constructs a new <see cref="ArrayBuilder{T}"/> with the specified minimum capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity to initialize with.</param>
        public ArrayBuilder(int capacity = 4)
#if NETSTANDARD2_1
            : this(ArrayPool<T>.Shared, capacity) { }
#else
        {
            Count = 0;
            array = new T[capacity];
            rented = false;
        }
#endif

#if NETSTANDARD2_1
        /// <summary>
        /// Constructs a new <see cref="ArrayBuilder{T}"/> with the specified <see cref="ArrayPool{T}"/>
        /// and minimumm capcacity.
        /// </summary>
        /// <param name="pool">The <see cref="ArrayPool{T}"/> to use to allocate new arrays.</param>
        /// <param name="capacity">The minimum capacity to start with.</param>
        public ArrayBuilder(ArrayPool<T> pool, int capacity = 4)
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            this.pool = pool;
            Count = 0;
            array = pool.Rent(capacity);
            rented = true;
        }
#endif

        /// <summary>
        /// Ensures the specified amount in the internal array.
        /// </summary>
        /// <param name="amount">The amount to reserve.</param>
        public void Reserve(int amount)
        {
            if (array != null && amount <= array.Length) return;

            var newSize = array is not null ? array.Length * 2 : 8;
            newSize = Math.Max(newSize, 8);
            while (amount > newSize)
                newSize *= 2;
            Resize(newSize);
        }

#if !NETSTANDARD2_1
        private void Resize(int newSize)
            => Array.Resize(ref array, newSize);
#else
        private void Resize(int newSize)
        {
            if (pool == null) pool = ArrayPool<T>.Shared;

            var newArr = pool.Rent(newSize); // perhaps this should ask for a bigger scale?
            if (array != null)
            {
                Array.Copy(array, newArr, Count);
                if (rented) pool.Return(array, true);
            }
            array = newArr;
            rented = true;
        }
#endif

        /// <summary>
        /// Clears the underlying array and frees it.
        /// </summary>
        public void Clear()
        {
            Count = 0;
            ClearInternal();
            array = Array.Empty<T>();
        }

#if !NETSTANDARD2_1
        [SuppressMessage("Performance", "CA1822:Mark members as static",
            Justification = "This is an instance method to keep consistency between builds.")]
#endif
        private void ClearInternal()
        {
#if NETSTANDARD2_1
            if (rented) pool.Return(array, true);
            rented = false;
#endif
        }

        /// <summary>
        /// Disposes of this <see cref="ArrayBuilder{T}"/>, releasing the underlying array if possible.
        /// </summary>
        public void Dispose() => Clear();

        /// <summary>
        /// Adds an item to the internal array.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            Reserve(Count + 1);
            array[Count++] = item;
        }

        /// <summary>
        /// Gets the item at the specified index in the builder.
        /// </summary>
        /// <param name="i">The index of the item to get.</param>
        /// <returns>The item at that index.</returns>
        public T this[int i] => array[i];

        /// <summary>
        /// Gets this builder as an array.
        /// </summary>
        /// <returns>The array that represents this builder.</returns>
        public T[] ToArray()
        {
            if (array == null) return Array.Empty<T>();
            if (rented || array.Length != Count)
            {
                var newArr = Count == 0 ? Array.Empty<T>() : new T[Count];
                Array.Copy(array, newArr, Count);
                ClearInternal();
                array = newArr;
            }
            return array;
        }
    }
}
