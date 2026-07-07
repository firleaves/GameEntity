using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class EventBusOptions
    {
        public int MaxQueuedEventCount = 4096;
        public int MaxFlushCountPerFrame = 1024;
        public bool ThrowHandlerException;

        public EventBusOptions Clone()
        {
            return new EventBusOptions
            {
                MaxQueuedEventCount = MaxQueuedEventCount,
                MaxFlushCountPerFrame = MaxFlushCountPerFrame,
                ThrowHandlerException = ThrowHandlerException
            };
        }

        public static EventBusOptions CreateDefault()
        {
            return new EventBusOptions();
        }
    }

}
