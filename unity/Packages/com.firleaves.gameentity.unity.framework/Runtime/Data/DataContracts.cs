using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public enum DataLoadState
    {
        Unloaded,
        Loading,
        Loaded,
        Failed
    }

    public interface IDataTable
    {
        string Name { get; }
        DataLoadState State { get; }
        Exception LastError { get; }

        UniTask LoadAsync(CancellationToken ct = default);
        UniTask ReloadAsync(CancellationToken ct = default);
        void Unload();
    }

    public interface IDataTable<T> : IDataTable where T : class
    {
        int Count { get; }
        T Get(string id);
        bool TryGet(string id, out T value);
        IReadOnlyCollection<T> GetAll();
        bool Contains(string id);
    }

    public interface IGameData
    {
        bool IsInitialized { get; }
        event Action<Type> TableReloaded;

        void Register<T>(IDataTable<T> table) where T : class;
        JsonDataTable<T> RegisterJson<T>(
            string location,
            Func<string, Dictionary<string, T>> parser,
            string packageName = null,
            bool rawFile = false)
            where T : class;

        ScriptableObjectDataTable<T> RegisterScriptableObject<T>(
            string location,
            string packageName = null,
            bool subAssets = true)
            where T : ScriptableObject;

        UniTask LoadAllAsync(CancellationToken ct = default);
        UniTask ReloadAsync<T>(CancellationToken ct = default) where T : class;
        T Get<T>(string id) where T : class;
        bool TryGet<T>(string id, out T value) where T : class;
        IReadOnlyCollection<T> GetAll<T>() where T : class;
        TTable GetTable<TTable>() where TTable : class, IDataTable;
    }
}
