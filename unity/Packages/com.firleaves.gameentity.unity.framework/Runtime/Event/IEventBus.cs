using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        IDisposable SubscribeOnce<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent evt);
        void Post<TEvent>(TEvent evt);
        int Flush(int maxCount = -1);
        void Clear<TEvent>();
        void ClearAll();
        EventBusSnapshot GetSnapshot();
    }

}
