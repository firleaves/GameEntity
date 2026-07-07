using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetPreloadOptions
    {
        public string Group;
        public float? ExpireSeconds;
        public IProgress<AssetPreloadProgress> Progress;
    }

}
