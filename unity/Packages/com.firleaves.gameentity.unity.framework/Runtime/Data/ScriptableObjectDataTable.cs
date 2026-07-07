using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class ScriptableObjectDataTable<T> : IDataTable<T> where T : ScriptableObject
    {
        private readonly IAssetPool _assetPool;
        private readonly AssetKey _key;
        private AssetRef<T> _mainRef;
        private SubAssetsRef<T> _subAssetsRef;
        private Dictionary<string, T> _data = new Dictionary<string, T>(StringComparer.Ordinal);

        public ScriptableObjectDataTable(IAssetPool assetPool, AssetKey key)
        {
            _assetPool = assetPool ?? throw new FrameworkException("ScriptableObjectDataTable 需要 AssetPool。");
            _key = key;
        }

        public string Name => $"ScriptableObject<{typeof(T).Name}>:{_key.Location}";
        public DataLoadState State { get; private set; } = DataLoadState.Unloaded;
        public Exception LastError { get; private set; }
        public int Count => _data.Count;

        public async UniTask LoadAsync(CancellationToken ct = default)
        {
            if (State == DataLoadState.Loading || State == DataLoadState.Loaded)
            {
                return;
            }

            State = DataLoadState.Loading;
            LastError = null;
            try
            {
                var next = new Dictionary<string, T>(StringComparer.Ordinal);
                if (_key.Kind == AssetKind.SubAssets)
                {
                    _subAssetsRef = await _assetPool.LoadSubAssetsAsync<T>(_key, ct: ct);
                    foreach (var asset in _subAssetsRef.Assets)
                    {
                        if (asset != null)
                        {
                            next[asset.name] = asset;
                        }
                    }
                }
                else
                {
                    _mainRef = await _assetPool.LoadAsync<T>(_key, ct: ct);
                    if (_mainRef.Asset != null)
                    {
                        next[_mainRef.Asset.name] = _mainRef.Asset;
                    }
                }

                _data = next;
                State = DataLoadState.Loaded;
            }
            catch (Exception ex)
            {
                LastError = ex;
                State = DataLoadState.Failed;
                throw;
            }
        }

        public async UniTask ReloadAsync(CancellationToken ct = default)
        {
            Unload();
            await LoadAsync(ct);
        }

        public void Unload()
        {
            _mainRef?.Release();
            _mainRef = null;
            _subAssetsRef?.Release();
            _subAssetsRef = null;
            _data = new Dictionary<string, T>(StringComparer.Ordinal);
            State = DataLoadState.Unloaded;
            LastError = null;
        }

        public T Get(string id)
        {
            if (TryGet(id, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"{Name} 找不到数据：{id}");
        }

        public bool TryGet(string id, out T value)
        {
            value = null;
            return !string.IsNullOrWhiteSpace(id) && _data.TryGetValue(id, out value);
        }

        public IReadOnlyCollection<T> GetAll()
        {
            return _data.Values;
        }

        public bool Contains(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && _data.ContainsKey(id);
        }
    }

}
