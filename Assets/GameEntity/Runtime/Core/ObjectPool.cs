using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GE
{
    public class ObjectPool : Singleton<ObjectPool>, ISingletonAwake
    {
        private ConcurrentDictionary<Type, Pool> _objPool;

        private readonly Func<Type, Pool> _addPoolFunc = type => new Pool(type, 1000);

        public void Awake()
        {
            lock (this)
            {
                _objPool = new ConcurrentDictionary<Type, Pool>();
            }
        }

        public T Fetch<T>() where T : class
        {
            return this.Fetch(typeof(T)) as T;
        }

        public object Fetch(Type type, bool isFromPool = true)
        {
            if (!isFromPool)
            {
                return Activator.CreateInstance(type);
            }

            Pool pool = GetPool(type);
            object obj = pool.Get();
            if (obj is IPool p)
            {
                p.IsFromPool = true;
            }
            return obj;
        }

        public void Recycle(object obj)
        {
            if (obj is IPool p)
            {
                if (!p.IsFromPool)
                {
                    return;
                }

                // 防止多次入池
                p.IsFromPool = false;
            }

            Type type = obj.GetType();
            Pool pool = GetPool(type);
            pool.Return(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Pool GetPool(Type type)
        {
            return this._objPool.GetOrAdd(type, _addPoolFunc);
        }
        


        /// <summary>
        /// 线程安全的无锁对象池
        /// </summary>
        private class Pool
        {
            private readonly Type _objectType;
            private readonly int _maxCapacity;
            private int _numItems;
            private readonly ConcurrentQueue<object> _items = new();
            private object _fastItem;

            public Pool(Type objectType, int maxCapacity)
            {
                _objectType = objectType;
                _maxCapacity = maxCapacity;
            }

            public object Get()
            {
                object item = _fastItem;
                if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
                {
                    if (_items.TryDequeue(out item))
                    {
                        Interlocked.Decrement(ref _numItems);
                        return item;
                    }

                    return Activator.CreateInstance(this._objectType);
                }

                return item;
            }

            public void Return(object obj)
            {
                if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
                {
                    if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
                    {
                        _items.Enqueue(obj);
                        return;
                    }

                    Interlocked.Decrement(ref _numItems);
                }
            }
        }
    }
}