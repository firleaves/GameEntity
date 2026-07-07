using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class PoolPolicy
    {
        public int Capacity = 32;
        public float ExpireSeconds = 60f;
        public float AutoReleaseIntervalSeconds = 10f;
        public int Priority;
        public bool Locked;

        public PoolPolicy Clone()
        {
            return new PoolPolicy
            {
                Capacity = Capacity,
                ExpireSeconds = ExpireSeconds,
                AutoReleaseIntervalSeconds = AutoReleaseIntervalSeconds,
                Priority = Priority,
                Locked = Locked
            };
        }

        public static PoolPolicy CreateDefault()
        {
            return new PoolPolicy();
        }
    }

}
