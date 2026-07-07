using System;
using System.Collections.Generic;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public abstract class AssetRefBase : IDisposable
    {
        private readonly Action<AssetKey> _release;

        protected AssetRefBase(AssetKey key, Action<AssetKey> release)
        {
            Key = key;
            _release = release;
            IsValid = true;
        }

        public AssetKey Key { get; }

        public bool IsValid { get; private set; }

        public void Release()
        {
            if (!IsValid)
            {
                return;
            }

            IsValid = false;
            _release?.Invoke(Key);
        }

        public void Dispose()
        {
            Release();
        }
    }

}
