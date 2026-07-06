using System;
using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class ObjectPoolTests
    {
        [Test]
        public void ReleaseUnused_ReleasesExpiredItems()
        {
            var now = DateTime.UtcNow;
            var item = new TestPoolItem(now.AddSeconds(-10));
            var pool = new ObjectPool<TestPoolItem>();
            pool.Register(item);

            var released = pool.ReleaseUnused(now, new ObjectPoolPolicy(capacity: 10, expireSeconds: 1f, usePriority: true));

            Assert.AreEqual(1, released);
            Assert.AreEqual(0, pool.Count);
            Assert.AreEqual(1, item.ReleaseCount);
            Assert.IsFalse(item.LastReleaseWasShutdown);
        }

        [Test]
        public void ReleaseUnused_DoesNotReleaseLockedItems()
        {
            var now = DateTime.UtcNow;
            var item = new TestPoolItem(now.AddSeconds(-10));
            var pool = new ObjectPool<TestPoolItem>(isLocked: candidate => candidate.Locked);
            item.Locked = true;
            pool.Register(item);

            var released = pool.ReleaseUnused(now, new ObjectPoolPolicy(capacity: 0, expireSeconds: 1f, usePriority: true));

            Assert.AreEqual(0, released);
            Assert.AreEqual(1, pool.Count);
            Assert.AreEqual(0, item.ReleaseCount);
        }

        [Test]
        public void ReleaseUnused_ReleasesOverCapacityItemsByPriorityThenLastUseTime()
        {
            var now = DateTime.UtcNow;
            var lowPriorityOld = new TestPoolItem(now.AddSeconds(-30)) { Priority = 1 };
            var lowPriorityNew = new TestPoolItem(now.AddSeconds(-10)) { Priority = 1 };
            var highPriorityOld = new TestPoolItem(now.AddSeconds(-40)) { Priority = 9 };
            var pool = new ObjectPool<TestPoolItem>(
                getPriority: candidate => candidate.Priority,
                getLastUseTimeUtc: candidate => candidate.LastUseTimeUtc);
            pool.Register(highPriorityOld);
            pool.Register(lowPriorityNew);
            pool.Register(lowPriorityOld);

            var released = pool.ReleaseUnused(now, new ObjectPoolPolicy(capacity: 1, expireSeconds: 0f, usePriority: true));

            Assert.AreEqual(2, released);
            Assert.AreEqual(1, pool.Count);
            Assert.AreEqual(1, lowPriorityOld.ReleaseCount);
            Assert.AreEqual(1, lowPriorityNew.ReleaseCount);
            Assert.AreEqual(0, highPriorityOld.ReleaseCount);
        }

        [Test]
        public void Shutdown_ReleasesAllItemsAsShutdown()
        {
            var now = DateTime.UtcNow;
            var first = new TestPoolItem(now);
            var second = new TestPoolItem(now) { InUse = true };
            var pool = new ObjectPool<TestPoolItem>();
            pool.Register(first);
            pool.Register(second);

            pool.Shutdown();

            Assert.AreEqual(0, pool.Count);
            Assert.AreEqual(1, first.ReleaseCount);
            Assert.AreEqual(1, second.ReleaseCount);
            Assert.IsTrue(first.LastReleaseWasShutdown);
            Assert.IsTrue(second.LastReleaseWasShutdown);
        }

        private sealed class TestPoolItem : IObjectPoolItem
        {
            public TestPoolItem(DateTime lastUseTimeUtc)
            {
                LastUseTimeUtc = lastUseTimeUtc;
            }

            public bool InUse;
            public bool Locked;
            public int Priority;
            public DateTime LastUseTimeUtc;
            public int ReleaseCount;
            public bool LastReleaseWasShutdown;

            public bool IsInUse => InUse;

            public bool CanRelease(DateTime now)
            {
                return true;
            }

            public void Release(bool isShutdown)
            {
                ReleaseCount++;
                LastReleaseWasShutdown = isShutdown;
            }
        }
    }
}
