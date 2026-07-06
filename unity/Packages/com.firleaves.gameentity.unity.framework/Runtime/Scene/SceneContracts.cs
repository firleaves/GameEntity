using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEntity.Unity.Framework
{
    public sealed class SceneLoadOptions
    {
        public string PackageName;
        public LoadSceneMode LoadMode = LoadSceneMode.Single;
        public LocalPhysicsMode PhysicsMode = LocalPhysicsMode.None;
        public bool SuspendLoad;
        public bool ActivateOnLoad = true;
        public uint Priority;
    }

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

    public sealed class SceneRef : IDisposable
    {
        private readonly Func<SceneRef, UniTask> _release;

        internal SceneRef(string location, string packageName, YooAsset.SceneHandle handle, Func<SceneRef, UniTask> release)
        {
            Location = location;
            PackageName = packageName;
            Handle = handle;
            _release = release;
            IsValid = handle != null && handle.IsValid;
        }

        public string Location { get; }
        public string PackageName { get; }
        public YooAsset.SceneHandle Handle { get; }
        public UnityEngine.SceneManagement.Scene Scene => Handle != null ? Handle.SceneObject : default;
        public bool IsValid { get; private set; }

        public bool Activate()
        {
            return Handle != null && Handle.ActivateScene();
        }

        public void Release()
        {
            ReleaseInternalAsync().Forget(Debug.LogException);
        }

        public UniTask ReleaseAsync()
        {
            return ReleaseInternalAsync();
        }

        private UniTask ReleaseInternalAsync()
        {
            if (!IsValid)
            {
                return UniTask.CompletedTask;
            }

            IsValid = false;
            return _release != null ? _release(this) : UniTask.CompletedTask;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
