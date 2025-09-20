using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Framework.Common.Function
{
    /// <summary>
    /// 对象池，内置创建逻辑以及销毁逻辑
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Func<T> _createFunction;
        private readonly Action<T> _destroyFunction;
        private readonly Queue<T> _objectQueue;
        private readonly int _maxSize;

        public ObjectPool(Func<T> createFunction, Action<T> destroyFunction = null, int defaultCapacity = 10,
            int maxSize = 100)
        {
            _createFunction = createFunction;
            _destroyFunction = destroyFunction;
            _objectQueue = new Queue<T>(defaultCapacity);
            _maxSize = maxSize;
        }

        /// <summary>
        /// 从缓存池中获取对象，如果没有则自行创建对象
        /// </summary>
        /// <param name="getAction"></param>
        /// <returns></returns>
        public T Get(UnityAction<T> getAction = null)
        {
            T obj;
            if (_objectQueue.Count > 0)
            {
                obj = _objectQueue.Dequeue();
                getAction?.Invoke(obj);
                return obj;
            }

            obj = _createFunction();
            getAction?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// 将对象放回缓存池，如果超过最大限制则直接销毁对象
        /// </summary>
        /// <param name="element"></param>
        /// <param name="releaseAction"></param>
        public void Release(T element, UnityAction<T> releaseAction = null)
        {
            // 去除重复元素
            if (_objectQueue.Contains(element))
            {
                return;
            }
            
            if (_objectQueue.Count >= _maxSize)
            {
                releaseAction?.Invoke(element);
                _destroyFunction?.Invoke(element);
                return;
            }

            releaseAction?.Invoke(element);
            _objectQueue.Enqueue(element);
        }

        /// <summary>
        /// 清空缓存池，并对对象执行销毁逻辑
        /// </summary>
        public void Clear()
        {
            if (_destroyFunction != null)
            {
                foreach (var obj in _objectQueue)
                {
                    _destroyFunction(obj);
                }
            }

            _objectQueue.Clear();
        }
    }
}