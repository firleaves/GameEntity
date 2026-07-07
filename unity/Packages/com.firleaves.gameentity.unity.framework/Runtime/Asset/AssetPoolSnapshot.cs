using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetPoolSnapshot
    {
        public DateTime CapturedAtUtc;
        public int CanReleaseCount;
        public IReadOnlyList<AssetInfo> Items;
    }

}
