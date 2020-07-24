using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Hive.Utilities
{
    public ref struct ArrayBuilder<T>
    {
        private ArrayPool<T> pool;
        private T[] array;
        public int Count { get; private set; }
        private bool rented;

        public ArrayBuilder(int capacity = 4) : this(ArrayPool<T>.Shared, capacity) { }
        public ArrayBuilder(ArrayPool<T> pool, int capacity = 4)
        {
            this.pool = pool;
            Count = 0;
            array = pool.Rent(capacity);
            rented = true;
        }

        public void Reserve(int amount)
        {
            if (amount < array.Length) return;

            var newArr = pool.Rent(amount); // perhaps this should ask for a bigger scale?
            Array.Copy(array, newArr, Count);
            if (rented) pool.Return(array, true);
            array = newArr;
            rented = true;
        }

        public void Add(T item)
        {
            Reserve(Count + 1);
            array[Count++] = item;
        }

        public T[] ToArray()
        {
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
