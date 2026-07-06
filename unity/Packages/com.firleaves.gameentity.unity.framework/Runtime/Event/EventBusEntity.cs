using System;
using System.Collections.Generic;
using GameEntity;

namespace GameEntity.Unity.Framework
{
    public sealed class EventBusEntity : Entity, IAwake, IDestroy, IEventBus
    {
        private readonly Dictionary<Type, List<Subscription>> _subscriptions = new Dictionary<Type, List<Subscription>>();
        private readonly List<Subscription> _dispatchBuffer = new List<Subscription>();

        public void Awake()
        {
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
            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            _dispatchBuffer.Clear();
            _dispatchBuffer.AddRange(list);
            for (var i = 0; i < _dispatchBuffer.Count; i++)
            {
                var subscription = _dispatchBuffer[i];
                if (!subscription.Active)
                {
                    continue;
                }

                subscription.Invoke(evt);
                if (subscription.Once)
                {
                    subscription.Dispose();
                }
            }

            _dispatchBuffer.Clear();
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
            _dispatchBuffer.Clear();
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
