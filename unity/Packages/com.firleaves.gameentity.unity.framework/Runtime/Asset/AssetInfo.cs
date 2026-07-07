using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetInfo
    {
        public AssetKey Key;
        public AssetLoadState LoadState;
        public int RefCount;
        public bool Locked;
        public int Priority;
        public DateTime LastUseTimeUtc;
        public DateTime? CacheUntilUtc;
        public IReadOnlyList<string> Groups;
        public string Error;
    }

}
