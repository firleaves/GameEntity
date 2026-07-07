using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AssetLoadOptions
    {
        public uint LoadPriority;
        public string Group;
        public bool Locked;
        public int Priority;
        public float? ExpireSeconds;

        public static readonly AssetLoadOptions Default = new AssetLoadOptions();
    }

}
