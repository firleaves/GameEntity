using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IDataTable<T> : IDataTable where T : class
    {
        int Count { get; }
        T Get(string id);
        bool TryGet(string id, out T value);
        IReadOnlyCollection<T> GetAll();
        bool Contains(string id);
    }

}
