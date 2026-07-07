using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IDataTable
    {
        string Name { get; }
        DataLoadState State { get; }
        Exception LastError { get; }

        UniTask LoadAsync(CancellationToken ct = default);
        UniTask ReloadAsync(CancellationToken ct = default);
        void Unload();
    }

}
