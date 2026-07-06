using System;
using System.Collections.Generic;

namespace GameEntity.Unity.Framework
{
    internal interface IObjectPoolItem
    {
        bool IsInUse { get; }
        bool CanRelease(DateTime now);
        void Release(bool isShutdown);
    }

    internal readonly struct ObjectPoolPolicy
    {
        public ObjectPoolPolicy(int capacity, float expireSeconds, bool usePriority)
        {
            Capacity = capacity;
            ExpireSeconds = expireSeconds;
            UsePriority = usePriority;
        }

        public int Capacity { get; }
        public float ExpireSeconds { get; }
        public bool UsePriority { get; }
    }

    internal sealed class ObjectPool<TItem> where TItem : class, IObjectPoolItem
    {
        private readonly List<TItem> _items = new List<TItem>();
        private readonly List<TItem> _candidates = new List<TItem>();
        private readonly Action<TItem> _onReleased;
        private readonly Func<TItem, bool> _isLocked;
        private readonly Func<TItem, int> _getPriority;
        private readonly Func<TItem, DateTime> _getLastUseTimeUtc;

        public ObjectPool(
            Action<TItem> onReleased = null,
            Func<TItem, bool> isLocked = null,
            Func<TItem, int> getPriority = null,
            Func<TItem, DateTime> getLastUseTimeUtc = null)
        {
            _onReleased = onReleased;
            _isLocked = isLocked;
            _getPriority = getPriority;
            _getLastUseTimeUtc = getLastUseTimeUtc;
        }

        public int Count => _items.Count;

        public void Register(TItem item)
        {
            if (item == null || _items.Contains(item))
            {
                return;
            }

            _items.Add(item);
        }

        public bool Unregister(TItem item)
        {
            return item != null && _items.Remove(item);
        }

        public void Clear()
        {
            _items.Clear();
            _candidates.Clear();
        }

        public int GetCanReleaseCount(DateTime now)
        {
            var count = 0;
            for (var i = 0; i < _items.Count; i++)
            {
                if (CanRelease(_items[i], now))
                {
                    count++;
                }
            }

            return count;
        }

        public int ReleaseUnused(DateTime now, ObjectPoolPolicy policy)
        {
            _candidates.Clear();
            CollectExpired(now, policy.ExpireSeconds, _candidates);

            var released = ReleaseCandidates(_candidates, false);
            _candidates.Clear();

            var overCapacity = policy.Capacity >= 0 ? Count - policy.Capacity : 0;
            if (overCapacity <= 0)
            {
                return released;
            }

            CollectReleasable(now, _candidates);
            SortCandidates(policy.UsePriority);
            for (var i = 0; i < _candidates.Count && overCapacity > 0; i++)
            {
                var item = _candidates[i];
                if (!_items.Remove(item))
                {
                    continue;
                }

                item.Release(false);
                _onReleased?.Invoke(item);
                overCapacity--;
                released++;
            }

            _candidates.Clear();
            return released;
        }

        public int ReleaseAllUnused(DateTime now)
        {
            _candidates.Clear();
            CollectReleasable(now, _candidates);
            var released = ReleaseCandidates(_candidates, false);
            _candidates.Clear();
            return released;
        }

        public void Shutdown()
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                _items[i].Release(true);
            }

            Clear();
        }

        private void CollectExpired(DateTime now, float expireSeconds, List<TItem> results)
        {
            if (expireSeconds <= 0f)
            {
                return;
            }

            var expireBefore = now.AddSeconds(-expireSeconds);
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (CanRelease(item, now) && GetLastUseTimeUtc(item) <= expireBefore)
                {
                    results.Add(item);
                }
            }
        }

        private void CollectReleasable(DateTime now, List<TItem> results)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (CanRelease(item, now))
                {
                    results.Add(item);
                }
            }
        }

        private int ReleaseCandidates(List<TItem> candidates, bool isShutdown)
        {
            var released = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                if (!_items.Remove(item))
                {
                    continue;
                }

                item.Release(isShutdown);
                _onReleased?.Invoke(item);
                released++;
            }

            return released;
        }

        private void SortCandidates(bool usePriority)
        {
            _candidates.Sort((a, b) =>
            {
                if (usePriority)
                {
                    var priority = GetPriority(a).CompareTo(GetPriority(b));
                    if (priority != 0)
                    {
                        return priority;
                    }
                }

                return GetLastUseTimeUtc(a).CompareTo(GetLastUseTimeUtc(b));
            });
        }

        private bool CanRelease(TItem item, DateTime now)
        {
            return item != null && !item.IsInUse && !IsLocked(item) && item.CanRelease(now);
        }

        private bool IsLocked(TItem item)
        {
            return _isLocked != null && _isLocked(item);
        }

        private int GetPriority(TItem item)
        {
            return _getPriority != null ? _getPriority(item) : 0;
        }

        private DateTime GetLastUseTimeUtc(TItem item)
        {
            return _getLastUseTimeUtc != null ? _getLastUseTimeUtc(item) : DateTime.MinValue;
        }
    }
}
