using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class InstanceRef : IDisposable
    {
        private readonly IInstancePool _pool;

        internal InstanceRef(AssetKey prefabKey, GameObject gameObject, IInstancePool pool)
        {
            PrefabKey = prefabKey;
            GameObject = gameObject;
            Transform = gameObject != null ? gameObject.transform : null;
            _pool = pool;
            IsValid = gameObject != null;
        }

        public AssetKey PrefabKey { get; }
        public GameObject GameObject { get; }
        public Transform Transform { get; }
        public bool IsValid { get; private set; }

        public void Return()
        {
            if (!IsValid)
            {
                return;
            }

            IsValid = false;
            _pool?.Return(this);
        }

        public void Dispose()
        {
            Return();
        }
    }

}
