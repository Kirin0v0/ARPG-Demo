using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.DataStructure
{
    /// <summary>
    /// 以小根堆为原型的优先级队列，最小的元素始终保持在队列的开头
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    public class PriorityQueue<T> : IEnumerable<T>, IEnumerable, ICollection
    {
        private readonly IComparer<T> _comparer;

        // 堆大小和数组容量
        private int _size;
        private int _capacity;
        public bool IsEmpty => _size == 0;
        public int Count => _size;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        // 堆数组
        private T[] _elements;
        private int Parent(int index) => (index - 1) / 2;
        private int LeftChild(int index) => index * 2 + 1;
        private int RightChild(int index) => index * 2 + 2;

        /// <summary>
        /// 共有构造器，用于从零创建优先级队列
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="capacity"></param>
        public PriorityQueue(IComparer<T> comparer, int capacity)
        {
            _comparer = comparer;
            _size = 0;
            _capacity = capacity;
            _elements = new T[capacity];
        }

        /// <summary>
        /// 私有构造器，用于复制已有优先级队列
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="array"></param>
        private PriorityQueue(IComparer<T> comparer, T[] array)
        {
            _comparer = comparer;
            _size = array.Length;
            _capacity = array.Length;
            _elements = array;
        }

        public void Enqueue(T element)
        {
            if (_size == _capacity)
            {
                ExpandCapacity();
            }

            _elements[_size] = element;
            HeapifyUp(_size);
            _size++;
        }

        public T Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("The queue is empty");
            }

            T element = _elements[0];
            (_elements[0], _elements[_size - 1]) = (_elements[_size - 1], _elements[0]);
            _size--;
            HeapifyDown(0);
            return element;
        }

        public bool TryDequeue(out T element)
        {
            if (_size == 0)
            {
                element = default(T);
                return false;
            }

            element = _elements[0];
            (_elements[0], _elements[_size - 1]) = (_elements[_size - 1], _elements[0]);
            _size--;
            HeapifyDown(0);
            return true;
        }

        public T Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("The queue is empty");
            }

            T element = _elements[0];
            return element;
        }

        public bool TryPeek(out T element)
        {
            if (_size == 0)
            {
                element = default(T);
                return false;
            }

            element = _elements[0];
            return true;
        }

        public void Sort()
        {
            Sort(_elements, _size);
        }

        public void Clear()
        {
            _size = 0;
            _elements = new T[_capacity];
        }

        /// <summary>
        /// 数组堆化
        /// </summary>
        /// <param name="array">未堆化的原数组</param>
        /// <returns></returns>
        private T[] Heapify(T[] array)
        {
            var copy = new T[array.Length];
            Array.Copy(array, copy, array.Length);
            Sort(copy, copy.Length);
            return copy;
        }

        /// <summary>
        /// 数组堆排序
        /// </summary>
        /// <param name="array">任意数组</param>
        /// <param name="size">数组有效容量</param>
        private void Sort(T[] array, int size)
        {
            // 从最后一个非叶节点开始逐个下沉，最终会达到小根堆的效果
            for (var i = size / 2 - 1; i >= 0; i--)
            {
                HeapifyDown(array, size, i);
            }
        }

        /// <summary>
        /// 队列数组的堆上浮
        /// </summary>
        /// <param name="index">上浮的堆节点</param>
        private void HeapifyUp(int index)
        {
            HeapifyUp(_elements, index);
        }

        /// <summary>
        /// 队列数组的堆下沉
        /// </summary>
        /// <param name="index">下沉的堆节点</param>
        private void HeapifyDown(int index)
        {
            HeapifyDown(_elements, _size, index);
        }

        /// <summary>
        /// 堆上浮
        /// </summary>
        /// <param name="elements">数组</param>
        /// <param name="index">上浮的堆节点</param>
        private void HeapifyUp(T[] elements, int index)
        {
            while (index > 0)
            {
                var parentIndex = Parent(index);
                if (_comparer.Compare(elements[parentIndex], elements[index]) <= 0)
                {
                    break;
                }

                (elements[parentIndex], elements[index]) = (elements[index], elements[parentIndex]);
                index = parentIndex;
            }
        }

        /// <summary>
        /// 堆下沉
        /// </summary>
        /// <param name="elements">数组</param>
        /// <param name="size">数组数量，数组长度不一定代表有效数量</param>
        /// <param name="index">下沉的堆节点</param>
        private void HeapifyDown(T[] elements, int size, int index)
        {
            while (index < size)
            {
                var smallest = index;
                var left = LeftChild(index);
                var right = RightChild(index);
                if (left < size && _comparer.Compare(elements[left], elements[smallest]) < 0)
                {
                    smallest = left;
                }

                if (right < size && _comparer.Compare(elements[right], elements[smallest]) < 0)
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    break;
                }

                (elements[smallest], elements[index]) = (elements[index], elements[smallest]);
                index = smallest;
            }
        }

        private void ExpandCapacity()
        {
            _capacity = Mathf.CeilToInt(_capacity * 1.5f);
            var newElements = new T[_capacity];
            Array.Copy(_elements, newElements, _elements.Length);
            _elements = newElements;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var clone = Clone();
            while (clone.TryDequeue(out var element))
            {
                yield return element;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Length - index < _size)
                throw new ArgumentException(
                    "Destination array is not long enough to copy all the items in the collection");

            var clone = Clone();
            while (clone.TryDequeue(out var element))
            {
                _elements[index++] = element;
            }
        }

        private PriorityQueue<T> Clone()
        {
            var elements = new T[_size];
            Array.Copy(_elements, elements, _size);
            var clone = new PriorityQueue<T>(_comparer, elements);
            return clone;
        }
    }
}