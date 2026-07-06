using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class EventBusTests
    {
        [Test]
        public void Publish_NotifiesSubscribers()
        {
            var bus = new EventBusEntity();
            bus.Awake();
            var received = 0;
            bus.Subscribe<TestEvent>(evt => received += evt.Value);

            bus.Publish(new TestEvent { Value = 3 });
            bus.Publish(new TestEvent { Value = 4 });

            Assert.AreEqual(7, received);
        }

        [Test]
        public void SubscribeOnce_NotifiesOnlyOnce()
        {
            var bus = new EventBusEntity();
            bus.Awake();
            var received = 0;
            bus.SubscribeOnce<TestEvent>(evt => received += evt.Value);

            bus.Publish(new TestEvent { Value = 2 });
            bus.Publish(new TestEvent { Value = 2 });

            Assert.AreEqual(2, received);
        }

        [Test]
        public void DisposedSubscription_DoesNotReceiveEvents()
        {
            var bus = new EventBusEntity();
            bus.Awake();
            var received = 0;
            var subscription = bus.Subscribe<TestEvent>(evt => received += evt.Value);

            subscription.Dispose();
            bus.Publish(new TestEvent { Value = 5 });

            Assert.AreEqual(0, received);
        }

        [Test]
        public void ClearAll_RemovesAllSubscriptions()
        {
            var bus = new EventBusEntity();
            bus.Awake();
            var received = 0;
            bus.Subscribe<TestEvent>(evt => received += evt.Value);
            bus.Subscribe<OtherEvent>(_ => received += 100);

            bus.ClearAll();
            bus.Publish(new TestEvent { Value = 1 });
            bus.Publish(new OtherEvent());

            Assert.AreEqual(0, received);
        }

        private struct TestEvent
        {
            public int Value;
        }

        private struct OtherEvent
        {
        }
    }
}
