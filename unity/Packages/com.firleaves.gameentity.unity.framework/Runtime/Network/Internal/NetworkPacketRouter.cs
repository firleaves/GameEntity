using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    internal sealed class NetworkPacketRouter
    {
        private readonly Dictionary<Type, List<Subscription>> _handlers = new Dictionary<Type, List<Subscription>>();

        public IDisposable Listen<TPacket>(Action<TPacket> handler)
        {
            if (handler == null)
            {
                throw new FrameworkException("注册网络消息监听失败：handler 不能为空。");
            }

            var type = typeof(TPacket);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Subscription>();
                _handlers.Add(type, list);
            }

            var subscription = new Subscription(this, type, packet => handler((TPacket)packet));
            list.Add(subscription);
            return subscription;
        }

        public void Dispatch(object packet)
        {
            if (packet == null)
            {
                return;
            }

            var type = packet.GetType();
            if (!_handlers.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            var snapshot = list.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                if (!snapshot[i].Disposed)
                {
                    snapshot[i].Invoke(packet);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        private void Remove(Subscription subscription)
        {
            if (subscription == null)
            {
                return;
            }

            if (_handlers.TryGetValue(subscription.PacketType, out var list))
            {
                list.Remove(subscription);
                if (list.Count == 0)
                {
                    _handlers.Remove(subscription.PacketType);
                }
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly NetworkPacketRouter _owner;
            private readonly Action<object> _handler;

            public Subscription(NetworkPacketRouter owner, Type packetType, Action<object> handler)
            {
                _owner = owner;
                PacketType = packetType;
                _handler = handler;
            }

            public Type PacketType { get; }
            public bool Disposed { get; private set; }

            public void Invoke(object packet)
            {
                _handler(packet);
            }

            public void Dispose()
            {
                if (Disposed)
                {
                    return;
                }

                Disposed = true;
                _owner.Remove(this);
            }
        }
    }
}
