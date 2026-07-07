using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IUIAdapter
    {
        UniTask<IUIView> CreateViewAsync(AssetKey viewKey, Transform parent, CancellationToken ct = default);
        void ReleaseView(IUIView view);
    }

}
