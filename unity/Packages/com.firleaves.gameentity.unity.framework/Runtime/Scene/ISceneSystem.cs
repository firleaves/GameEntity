using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEntity.Unity.Framework
{
    public interface ISceneSystem
    {
        string ActiveSceneLocation { get; }
        IReadOnlyCollection<SceneRef> LoadedScenes { get; }

        UniTask<SceneRef> LoadSceneAsync(
            string location,
            SceneLoadOptions options = null,
            CancellationToken ct = default);

        UniTask<SceneRef> ChangeSceneAsync(
            string location,
            SceneLoadOptions options = null,
            CancellationToken ct = default);

        UniTask<bool> UnloadSceneAsync(string location, CancellationToken ct = default);
        bool TryGetScene(string location, out SceneRef sceneRef);
    }

}
