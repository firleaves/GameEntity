using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class EventBusTests
    {
        [Test]
        public void Publish_NotifiesSubscribers()
        {
            var bus = new EventBusEntity();
            bus.Awake(EventBusOptions.CreateDefault());
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
            bus.Awake(EventBusOptions.CreateDefault());
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
            bus.Awake(EventBusOptions.CreateDefault());
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
            bus.Awake(EventBusOptions.CreateDefault());
            var received = 0;
            bus.Subscribe<TestEvent>(evt => received += evt.Value);
            bus.Subscribe<OtherEvent>(_ => received += 100);

            bus.ClearAll();
            bus.Publish(new TestEvent { Value = 1 });
            bus.Publish(new OtherEvent());

            Assert.AreEqual(0, received);
        }

        [Test]
        public void Post_DoesNotDispatchUntilFlush()
        {
            var bus = new EventBusEntity();
            bus.Awake(EventBusOptions.CreateDefault());
            var received = 0;
            bus.Subscribe<TestEvent>(evt => received += evt.Value);

            bus.Post(new TestEvent { Value = 3 });
            Assert.AreEqual(0, received);

            Assert.AreEqual(1, bus.Flush());
            Assert.AreEqual(3, received);
        }

        [Test]
        public void Update_FlushesQueuedEventsWithFrameLimit()
        {
            var bus = new EventBusEntity();
            bus.Awake(new EventBusOptions
            {
                MaxFlushCountPerFrame = 1
            });
            var received = 0;
            bus.Subscribe<TestEvent>(evt => received += evt.Value);

            bus.Post(new TestEvent { Value = 1 });
            bus.Post(new TestEvent { Value = 2 });

            bus.Update(0.016f);
            Assert.AreEqual(1, received);
            Assert.AreEqual(1, bus.GetSnapshot().QueuedEventCount);

            bus.Update(0.016f);
            Assert.AreEqual(3, received);
            Assert.AreEqual(0, bus.GetSnapshot().QueuedEventCount);
        }

        [Test]
        public void Post_Throws_WhenQueueIsFull()
        {
            var bus = new EventBusEntity();
            bus.Awake(new EventBusOptions
            {
                MaxQueuedEventCount = 1
            });

            bus.Post(new TestEvent { Value = 1 });

            Assert.Throws<FrameworkException>(() => bus.Post(new TestEvent { Value = 2 }));
        }

        [Test]
        public void HandlerException_DoesNotStopOtherSubscribers()
        {
            var bus = new EventBusEntity();
            bus.Awake(EventBusOptions.CreateDefault());
            var received = 0;
            bus.Subscribe<TestEvent>(_ => throw new System.InvalidOperationException("test"));
            bus.Subscribe<TestEvent>(evt => received += evt.Value);

            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Exception,
                new Regex("InvalidOperationException: test"));
            bus.Publish(new TestEvent { Value = 5 });

            Assert.AreEqual(5, received);
        }

        [Test]
        public void Publish_IsSafe_WhenHandlerPublishesAnotherEvent()
        {
            var bus = new EventBusEntity();
            bus.Awake(EventBusOptions.CreateDefault());
            var received = 0;
            bus.Subscribe<TestEvent>(_ => bus.Publish(new OtherEvent()));
            bus.Subscribe<TestEvent>(_ => received += 1);
            bus.Subscribe<OtherEvent>(_ => received += 10);

            bus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(11, received);
        }

        [Test]
        public void GetSnapshot_ReturnsSubscriberAndQueueCounts()
        {
            var bus = new EventBusEntity();
            bus.Awake(EventBusOptions.CreateDefault());
            bus.Subscribe<TestEvent>(_ => { });
            bus.Subscribe<OtherEvent>(_ => { });
            bus.Post(new TestEvent { Value = 1 });

            var snapshot = bus.GetSnapshot();

            Assert.AreEqual(2, snapshot.SubscriberCount);
            Assert.AreEqual(2, snapshot.EventTypeCount);
            Assert.AreEqual(1, snapshot.QueuedEventCount);
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
