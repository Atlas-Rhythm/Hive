using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Hive.Utilities
{
    /// <summary>
    /// A type that can be used to (fairly) efficitently create arrays with minimal allocation.
    /// </summary>
    /// <remarks>
    /// Before going out of scope, each instance <b>must</b> have either <see cref="ToArray"/> or <see cref="Clear"/> called
    /// to ensure that the underlying array is returned to the <see cref="ArrayPool{T}"/> it was rented from.
    /// </remarks>
    /// <typeparam name="T">The type of the array elements.</typeparam>
    public ref struct ArrayBuilder<T>
    {
        private ArrayPool<T> pool;
        private T[] array;
        /// <summary>
        /// The number of items currently in the <see cref="ArrayBuilder{T}"/>.
        /// </summary>
        public int Count { get; private set; }
        private bool rented;

        /// <summary>
        /// Constructs a new <see cref="ArrayBuilder{T}"/> with the specified minimum capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity to initialize with.</param>
        public ArrayBuilder(int capacity = 4) : this(ArrayPool<T>.Shared, capacity) { }
        /// <summary>
        /// Constructs a new <see cref="ArrayBuilder{T}"/> with the specified <see cref="ArrayPool{T}"/>
        /// and minimumm capcacity.
        /// </summary>
        /// <param name="pool">The <see cref="ArrayPool{T}"/> to use to allocate new arrays.</param>
        /// <param name="capacity">The minimum capacity to start with.</param>
        public ArrayBuilder(ArrayPool<T> pool, int capacity = 4)
        {
            this.pool = pool;
            Count = 0;
            array = pool.Rent(capacity);
            rented = true;
        }

        /// <summary>
        /// Ensures the specified amount in the internal array.
        /// </summary>
        /// <param name="amount">The amount to reserve.</param>
        public void Reserve(int amount)
        {
            if (array != null && amount < array.Length) return;
            if (pool == null) pool = ArrayPool<T>.Shared;

            var newArr = pool.Rent(amount); // perhaps this should ask for a bigger scale?
            if (array != null)
            {
                Array.Copy(array, newArr, Count);
                if (rented) pool.Return(array, true);
            }
            array = newArr;
            rented = true;
        }

        /// <summary>
        /// Clears the underlying array and frees it.
        /// </summary>
        public void Clear()
        {
            Count = 0;
            if (rented) pool.Return(array, true);
            array = Array.Empty<T>();
            rented = false;
        }

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
        /// Gets this builder as an array.
        /// </summary>
        /// <returns>The array that represents this builder.</returns>
        public T[] ToArray()
        {
            if (array == null) return Array.Empty<T>();
            if (rented || array.Length != Count)
            {
                var newArr = new T[Count];
                Array.Copy(array, newArr, Count);
                if (rented) pool.Return(array, true);
                array = newArr;
                rented = false;
            }
            return array;
        }
    }
}
