using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class JsonDataTable<T> : IDataTable<T> where T : class
    {
        private readonly IAssetPool _assetPool;
        private readonly AssetKey _key;
        private readonly Func<string, Dictionary<string, T>> _parser;
        private Dictionary<string, T> _data = new Dictionary<string, T>(StringComparer.Ordinal);

        public JsonDataTable(IAssetPool assetPool, AssetKey key, Func<string, Dictionary<string, T>> parser)
        {
            _assetPool = assetPool ?? throw new FrameworkException("JsonDataTable 需要 AssetPool。");
            _key = key;
            _parser = parser ?? throw new FrameworkException("JsonDataTable 需要 parser。");
        }

        public string Name => $"Json<{typeof(T).Name}>:{_key.Location}";
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
                var json = await LoadJsonAsync(ct);
                _data = _parser(json) ?? new Dictionary<string, T>(StringComparer.Ordinal);
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

        private async UniTask<string> LoadJsonAsync(CancellationToken ct)
        {
            if (_key.Kind == AssetKind.RawFile)
            {
                using (var raw = await _assetPool.LoadRawFileAsync(_key, ct: ct))
                {
                    return raw.GetRawFileText();
                }
            }

            using (var textAsset = await _assetPool.LoadAsync<TextAsset>(_key, ct: ct))
            {
                return textAsset.Asset != null ? textAsset.Asset.text : null;
            }
        }
    }

}
