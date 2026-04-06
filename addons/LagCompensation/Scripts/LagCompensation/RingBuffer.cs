using System;
using System.Collections;
using System.Collections.Generic;

// inpsired by https://github.com/RolandKoenig/SeeingSharp2/blob/master/src/SeeingSharp/Util/_Collections/RingBuffer.cs

namespace PG.LagCompensation.Base
{
    /// <summary>
    /// Simple implementation of a ring buffer, also known as circular buffer, see https://en.wikipedia.org/wiki/Circular_buffer
    /// Use this instead of a List<T>, as the RemoveAt(0) method gets increasinly slower the larger the buffer is.
    /// Internally uilizes a fixed length array as well as two integers.
    /// </summary>
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        /// <summary>
        /// Next 'Add' index
        /// </summary>
        private int _indexer;
        /// <summary>
        /// Current length/count of buffer. Will always be <= Capacity
        /// </summary>
        private int _count;

        public int Capacity => _buffer.Length;
        public int Count => _count;
        public bool IsFull => _count == Capacity;
        public bool IsEmpty => _count == 0;

        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than 0.");

            _buffer = new T[capacity];
            _indexer = 0;
            _count = 0;
        }

        /// <summary>
        /// Add item (overwrites oldest if full)
        /// </summary>
        public void Add(T item)
        {
            _buffer[_indexer] = item;
            _indexer = (_indexer + 1) % Capacity;

            if (_count < Capacity)
                _count++;
        }

        /// <summary>
        /// Get item by index (0 = oldest)
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int actualIndex = (_indexer - _count + index + Capacity) % Capacity;
                return _buffer[actualIndex];
            }
        }

        /// <summary>
        /// Remove oldest item and return it
        /// </summary>
        public T RemoveOldest()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Buffer is empty.");

            int tailIndex = (_indexer - _count + Capacity) % Capacity;
            T item = _buffer[tailIndex];
            _count--;
            return item;
        }

        /// <summary>
        /// Clear the buffer. Will not actually reset the items in the buffer but only reset the two integers, making it highly performant.
        /// </summary>
        public void Clear()
        {
            _indexer = 0;
            _count = 0;
        }
    }
}