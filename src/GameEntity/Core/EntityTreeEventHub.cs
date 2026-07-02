using System;
using System.Collections.Generic;

namespace GameEntity
{
    /// <summary>
    /// Entity 树事件中心。core 只发布实体语义事件，不直接创建任何引擎对象。
    /// </summary>
    public static class EntityTreeEventHub
    {
        private static readonly object LockObj = new object();
        private static readonly List<IEntityTreeObserver> Observers = new List<IEntityTreeObserver>();

        public static IDisposable Register(IEntityTreeObserver observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (LockObj)
            {
                Observers.Add(observer);
            }

            return new Registration(observer);
        }

        public static void Unregister(IEntityTreeObserver observer)
        {
            if (observer == null)
            {
                return;
            }

            lock (LockObj)
            {
                Observers.Remove(observer);
            }
        }

        internal static void NotifyEntityRegistered(Entity entity)
        {
            foreach (IEntityTreeObserver observer in Snapshot())
            {
                try
                {
                    observer.OnEntityRegistered(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"Entity tree observer register error: {e}");
                }
            }
        }

        internal static void NotifyEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
        {
            foreach (IEntityTreeObserver observer in Snapshot())
            {
                try
                {
                    observer.OnEntityParentChanged(entity, oldParent, newParent);
                }
                catch (Exception e)
                {
                    Log.Error($"Entity tree observer parent error: {e}");
                }
            }
        }

        internal static void NotifyEntityDestroyed(Entity entity)
        {
            foreach (IEntityTreeObserver observer in Snapshot())
            {
                try
                {
                    observer.OnEntityDestroyed(entity);
                }
                catch (Exception e)
                {
                    Log.Error($"Entity tree observer destroy error: {e}");
                }
            }
        }

        private static IEntityTreeObserver[] Snapshot()
        {
            lock (LockObj)
            {
                return Observers.ToArray();
            }
        }

        private sealed class Registration : IDisposable
        {
            private IEntityTreeObserver _observer;

            public Registration(IEntityTreeObserver observer)
            {
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer == null)
                {
                    return;
                }

                Unregister(_observer);
                _observer = null;
            }
        }
    }
}
