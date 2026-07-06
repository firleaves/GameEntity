using System;

namespace GameEntity.Unity.Framework
{
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        IDisposable SubscribeOnce<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent evt);
        void Clear<TEvent>();
        void ClearAll();
    }
}
