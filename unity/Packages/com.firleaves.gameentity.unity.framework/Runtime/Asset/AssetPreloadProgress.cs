using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public readonly struct AssetPreloadProgress
    {
        public readonly string Group;
        public readonly AssetKey Key;
        public readonly int CompletedCount;
        public readonly int TotalCount;
        public readonly float Progress;

        public AssetPreloadProgress(string group, AssetKey key, int completedCount, int totalCount)
        {
            Group = group;
            Key = key;
            CompletedCount = completedCount;
            TotalCount = totalCount;
            Progress = totalCount <= 0 ? 1f : Mathf.Clamp01((float)completedCount / totalCount);
        }
    }

}
