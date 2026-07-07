using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public readonly struct InstanceRentContext
    {
        public readonly AssetKey PrefabKey;
        public readonly bool FromPool;

        public InstanceRentContext(AssetKey prefabKey, bool fromPool)
        {
            PrefabKey = prefabKey;
            FromPool = fromPool;
        }
    }

}
