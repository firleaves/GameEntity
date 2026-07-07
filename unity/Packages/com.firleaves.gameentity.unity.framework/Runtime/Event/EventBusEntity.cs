using System;
using System.Collections.Generic;
using GameEntity;

namespace GameEntity.Unity.Framework
{
    public sealed class EventBusEntity : Entity, IAwake<EventBusOptions>, IUpdate, IDestroy, IEventBus
    {
        private readonly Dictionary<Type, List<Subscription>> _subscriptions = new Dictionary<Type, List<Subscription>>();
        private readonly Queue<QueuedEvent> _queue = new Queue<QueuedEvent>();
        private EventBusOptions _options;

        public void Awake(EventBusOptions options)
        {
            _options = options != null ? options.Clone() : EventBusOptions.CreateDefault();
        }

        public void Update(float deltaTime)
        {
            Flush(_options.MaxFlushCountPerFrame);
        }

        public void OnDestroy()
        {
            ClearAll();
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            return SubscribeInternal(handler, once: false);
        }

        public IDisposable SubscribeOnce<TEvent>(Action<TEvent> handler)
        {
            return SubscribeInternal(handler, once: true);
        }

        public void Publish<TEvent>(TEvent evt)
        {
            Publish(typeof(TEvent), evt);
        }

        public void Post<TEvent>(TEvent evt)
        {
            if (_options.MaxQueuedEventCount > 0 && _queue.Count >= _options.MaxQueuedEventCount)
            {
                throw new FrameworkException($"事件队列已满：{_queue.Count}/{_options.MaxQueuedEventCount}");
            }

            _queue.Enqueue(new QueuedEvent(typeof(TEvent), evt));
        }

        public int Flush(int maxCount = -1)
        {
            if (_queue.Count == 0)
            {
                return 0;
            }

            var count = 0;
            var limit = maxCount < 0 ? int.MaxValue : maxCount;
            while (_queue.Count > 0 && count < limit)
            {
                var queued = _queue.Dequeue();
                Publish(queued.EventType, queued.Event);
                count++;
            }

            return count;
        }

        public void Clear<TEvent>()
        {
            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var list))
            {
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                list[i].Active = false;
            }

            _subscriptions.Remove(type);
        }

        public void ClearAll()
        {
            foreach (var pair in _subscriptions)
            {
                var list = pair.Value;
                for (var i = 0; i < list.Count; i++)
                {
                    list[i].Active = false;
                }
            }

            _subscriptions.Clear();
            _queue.Clear();
        }

        public EventBusSnapshot GetSnapshot()
        {
            var types = new List<EventBusTypeInfo>(_subscriptions.Count);
            var subscriberCount = 0;
            foreach (var pair in _subscriptions)
            {
                var activeCount = 0;
                var list = pair.Value;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Active)
                    {
                        activeCount++;
                    }
                }

                if (activeCount <= 0)
                {
                    continue;
                }

                subscriberCount += activeCount;
                types.Add(new EventBusTypeInfo
                {
                    EventType = pair.Key,
                    SubscriberCount = activeCount
                });
            }

            return new EventBusSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                SubscriberCount = subscriberCount,
                EventTypeCount = types.Count,
                QueuedEventCount = _queue.Count,
                Types = types
            };
        }

        private IDisposable SubscribeInternal<TEvent>(Action<TEvent> handler, bool once)
        {
            if (handler == null)
            {
                throw new FrameworkException("订阅事件失败：handler 不能为空。");
            }

            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var list))
            {
                list = new List<Subscription>();
                _subscriptions.Add(type, list);
            }

            var subscription = new Subscription(list, evt => handler((TEvent)evt), once);
            list.Add(subscription);
            return subscription;
        }

        private void Publish(Type type, object evt)
        {
            if (!_subscriptions.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            var snapshot = list.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                var subscription = snapshot[i];
                if (!subscription.Active)
                {
                    continue;
                }

                if (subscription.Once)
                {
                    subscription.Dispose();
                }

                try
                {
                    subscription.Invoke(evt);
                }
                catch (Exception ex)
                {
                    if (_options.ThrowHandlerException)
                    {
                        throw;
                    }

                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        private readonly struct QueuedEvent
        {
            public readonly Type EventType;
            public readonly object Event;

            public QueuedEvent(Type eventType, object evt)
            {
                EventType = eventType;
                Event = evt;
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<Subscription> _owner;
            private readonly Action<object> _handler;

            public Subscription(List<Subscription> owner, Action<object> handler, bool once)
            {
                _owner = owner;
                _handler = handler;
                Once = once;
                Active = true;
            }

            public bool Once { get; }
            public bool Active { get; set; }

            public void Invoke(object evt)
            {
                _handler(evt);
            }

            public void Dispose()
            {
                if (!Active)
                {
                    return;
                }

                Active = false;
                _owner.Remove(this);
            }
        }
    }
}
