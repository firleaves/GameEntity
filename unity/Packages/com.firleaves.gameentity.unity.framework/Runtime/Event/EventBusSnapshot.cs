using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    public sealed class EventBusSnapshot
    {
        public DateTime CapturedAtUtc;
        public int SubscriberCount;
        public int EventTypeCount;
        public int QueuedEventCount;
        public IReadOnlyList<EventBusTypeInfo> Types;
    }

}
