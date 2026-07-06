using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class GameDataEntity : Entity, IAwake<IAssetPool>, IDestroy, IGameData
    {
        private readonly Dictionary<Type, IDataTable> _tablesByDataType = new Dictionary<Type, IDataTable>();
        private readonly Dictionary<Type, IDataTable> _tablesByTableType = new Dictionary<Type, IDataTable>();
        private IAssetPool _assetPool;

        public bool IsInitialized { get; private set; }
        public event Action<Type> TableReloaded;

        public void Awake(IAssetPool assetPool)
        {
            _assetPool = assetPool ?? throw new FrameworkException("GameData 初始化失败：AssetPool 不能为空。");
            IsInitialized = true;
        }

        public void OnDestroy()
        {
            foreach (var table in _tablesByTableType.Values)
            {
                table.Unload();
            }

            _tablesByDataType.Clear();
            _tablesByTableType.Clear();
            _assetPool = null;
            IsInitialized = false;
        }

        public void Register<T>(IDataTable<T> table) where T : class
        {
            if (table == null)
            {
                throw new FrameworkException($"注册数据表失败：{typeof(T).Name} table 不能为空。");
            }

            _tablesByDataType[typeof(T)] = table;
            _tablesByTableType[table.GetType()] = table;
        }

        public JsonDataTable<T> RegisterJson<T>(
            string location,
            Func<string, Dictionary<string, T>> parser,
            string packageName = null,
            bool rawFile = false)
            where T : class
        {
            var key = rawFile ? AssetKey.RawFile(location, packageName) : AssetKey.Main<TextAsset>(location, packageName);
            var table = new JsonDataTable<T>(_assetPool, key, parser);
            Register(table);
            return table;
        }

        public ScriptableObjectDataTable<T> RegisterScriptableObject<T>(
            string location,
            string packageName = null,
            bool subAssets = true)
            where T : ScriptableObject
        {
            var key = subAssets ? AssetKey.SubAssets<T>(location, packageName) : AssetKey.Main<T>(location, packageName);
            var table = new ScriptableObjectDataTable<T>(_assetPool, key);
            Register(table);
            return table;
        }

        public async UniTask LoadAllAsync(CancellationToken ct = default)
        {
            var tasks = new List<UniTask>(_tablesByTableType.Count);
            foreach (var table in _tablesByTableType.Values)
            {
                tasks.Add(table.LoadAsync(ct));
            }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask ReloadAsync<T>(CancellationToken ct = default) where T : class
        {
            var table = GetTypedTable<T>();
            await table.ReloadAsync(ct);
            TableReloaded?.Invoke(typeof(T));
        }

        public T Get<T>(string id) where T : class
        {
            return GetTypedTable<T>().Get(id);
        }

        public bool TryGet<T>(string id, out T value) where T : class
        {
            value = null;
            return _tablesByDataType.TryGetValue(typeof(T), out var table)
                && table is IDataTable<T> typed
                && typed.TryGet(id, out value);
        }

        public IReadOnlyCollection<T> GetAll<T>() where T : class
        {
            return GetTypedTable<T>().GetAll();
        }

        public TTable GetTable<TTable>() where TTable : class, IDataTable
        {
            return _tablesByTableType.TryGetValue(typeof(TTable), out var table)
                ? table as TTable
                : null;
        }

        private IDataTable<T> GetTypedTable<T>() where T : class
        {
            if (_tablesByDataType.TryGetValue(typeof(T), out var table) && table is IDataTable<T> typed)
            {
                return typed;
            }

            throw new FrameworkException($"未注册数据表：{typeof(T).Name}");
        }
    }
}
