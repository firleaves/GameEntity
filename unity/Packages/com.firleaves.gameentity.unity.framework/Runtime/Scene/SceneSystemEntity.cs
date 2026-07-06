using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using YooAsset;

namespace GameEntity.Unity.Framework
{
    public sealed class SceneSystemEntity : Entity, IAwake<IYooAssetBootstrap>, IDestroy, ISceneSystem
    {
        private readonly Dictionary<string, SceneRef> _loadedScenes = new Dictionary<string, SceneRef>(StringComparer.Ordinal);
        private IYooAssetBootstrap _bootstrap;

        public string ActiveSceneLocation { get; private set; }
        public IReadOnlyCollection<SceneRef> LoadedScenes => _loadedScenes.Values;

        public void Awake(IYooAssetBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new FrameworkException("SceneSystem 初始化失败：YooAssetBootstrap 不能为空。");
        }

        public void OnDestroy()
        {
            foreach (var pair in _loadedScenes)
            {
                pair.Value.Handle?.Release();
            }

            _loadedScenes.Clear();
            ActiveSceneLocation = null;
            _bootstrap = null;
        }

        public async UniTask<SceneRef> LoadSceneAsync(string location, SceneLoadOptions options = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new FrameworkException("加载场景失败：location 不能为空。");
            }

            if (_loadedScenes.TryGetValue(location, out var existing) && existing.IsValid)
            {
                return existing;
            }

            options = options ?? new SceneLoadOptions();
            var package = _bootstrap.GetPackage(options.PackageName);
            var handle = package.LoadSceneAsync(
                location,
                options.LoadMode,
                options.PhysicsMode,
                options.SuspendLoad,
                options.Priority);

            var task = handle.Task;
            if (task != null)
            {
                await task.AsUniTask().AttachExternalCancellation(ct);
            }

            if (handle.Status != EOperationStatus.Succeed)
            {
                var error = !string.IsNullOrWhiteSpace(handle.LastError) ? handle.LastError : "未知错误";
                handle.Release();
                throw new FrameworkException($"加载场景失败：Location={location}, Error={error}");
            }

            var sceneRef = new SceneRef(location, options.PackageName, handle, ReleaseScene);
            _loadedScenes[location] = sceneRef;
            if (options.ActivateOnLoad)
            {
                sceneRef.Activate();
                ActiveSceneLocation = location;
            }

            return sceneRef;
        }

        public async UniTask<SceneRef> ChangeSceneAsync(string location, SceneLoadOptions options = null, CancellationToken ct = default)
        {
            options = options ?? new SceneLoadOptions();
            options.LoadMode = UnityEngine.SceneManagement.LoadSceneMode.Single;
            var previous = new List<string>(_loadedScenes.Keys);
            var sceneRef = await LoadSceneAsync(location, options, ct);
            for (var i = 0; i < previous.Count; i++)
            {
                if (!string.Equals(previous[i], location, StringComparison.Ordinal))
                {
                    await UnloadSceneAsync(previous[i], ct);
                }
            }

            return sceneRef;
        }

        public async UniTask<bool> UnloadSceneAsync(string location, CancellationToken ct = default)
        {
            if (!_loadedScenes.TryGetValue(location, out var sceneRef))
            {
                return false;
            }

            _loadedScenes.Remove(location);
            if (string.Equals(ActiveSceneLocation, location, StringComparison.Ordinal))
            {
                ActiveSceneLocation = null;
            }

            if (sceneRef.Handle != null && sceneRef.Handle.IsValid)
            {
                var operation = sceneRef.Handle.UnloadAsync();
                await operation.Task.AsUniTask().AttachExternalCancellation(ct);
                if (operation.Status != EOperationStatus.Succeed)
                {
                    throw new FrameworkException($"卸载场景失败：Location={location}, Error={operation.Error}");
                }
            }

            return true;
        }

        public bool TryGetScene(string location, out SceneRef sceneRef)
        {
            return _loadedScenes.TryGetValue(location, out sceneRef) && sceneRef.IsValid;
        }

        private UniTask ReleaseScene(SceneRef sceneRef)
        {
            if (sceneRef == null)
            {
                return UniTask.CompletedTask;
            }

            return UnloadSceneAsync(sceneRef.Location);
        }
    }
}
