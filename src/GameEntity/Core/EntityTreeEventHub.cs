using System;
using System.Collections.Generic;

namespace GameEntity
{
    /// <summary>
    /// Entity 树事件中心。core 只发布实体语义事件，不直接创建任何引擎对象。
    /// </summary>
    internal sealed class EntityTreeEventHub
    {
        private readonly object _lock = new object();
        private readonly List<IEntityTreeObserver> _observers = new List<IEntityTreeObserver>();

        public IDisposable Register(IEntityTreeObserver observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (_lock)
            {
                _observers.Add(observer);
            }

            return new Registration(this, observer);
        }

        public void Unregister(IEntityTreeObserver observer)
        {
            if (observer == null)
            {
                return;
            }

            lock (_lock)
            {
                _observers.Remove(observer);
            }
        }

        public void ReplayRegistered(IEntityTreeObserver observer, IEnumerable<Entity> entities)
        {
            if (observer == null || entities == null)
            {
                return;
            }

            foreach (Entity entity in entities)
            {
                NotifyObserver(observer, entity, static (target, value) => target.OnEntityRegistered(value), "replay");
            }
        }

        public void NotifyEntityRegistered(Entity entity)
        {
            foreach (IEntityTreeObserver observer in Snapshot())
            {
                NotifyObserver(observer, entity, static (target, value) => target.OnEntityRegistered(value), "register");
            }
        }

        public void NotifyEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
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

        public void NotifyEntityDestroyed(Entity entity)
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

        public void Clear()
        {
            lock (_lock)
            {
                _observers.Clear();
            }
        }

        private IEntityTreeObserver[] Snapshot()
        {
            lock (_lock)
            {
                return _observers.ToArray();
            }
        }

        private static void NotifyObserver(
            IEntityTreeObserver observer,
            Entity entity,
            Action<IEntityTreeObserver, Entity> callback,
            string operation)
        {
            try
            {
                callback(observer, entity);
            }
            catch (Exception e)
            {
                Log.Error($"Entity tree observer {operation} error: {e}");
            }
        }

        private sealed class Registration : IDisposable
        {
            private EntityTreeEventHub _hub;
            private IEntityTreeObserver _observer;

            public Registration(EntityTreeEventHub hub, IEntityTreeObserver observer)
            {
                _hub = hub;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer == null)
                {
                    return;
                }

                _hub.Unregister(_observer);
                _hub = null;
                _observer = null;
            }
        }
    }
}
