using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using GameEntity.Unity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class FrameworkEntry : MonoBehaviour
    {
        [SerializeField]
        private bool autoInitializeOnAwake = true;

        [SerializeField]
        private FrameworkOptions options = FrameworkOptions.CreateDefault();

        [SerializeField]
        private FrameworkExtensionAsset[] extensions = System.Array.Empty<FrameworkExtensionAsset>();

        private FrameworkScene _scene;
        private Transform _runtimeRoot;
        private CancellationTokenSource _destroyCts;
        private CancellationTokenSource _initializeCts;
        private UniTaskCompletionSource _initializeCompletion;
        private UniTaskCompletionSource _shutdownCompletion;
        private bool _initializing;
        private bool _shuttingDown;
        private bool _shutdownCompleted;

        public FrameworkScene Scene => _scene;
        public bool IsReady { get; private set; }
        public FrameworkOptions Options => options;

        private const string FrameworkSceneName = "GameEntity.Unity.Framework";

        private void Awake()
        {
            _destroyCts = new CancellationTokenSource();
            EnsureGameEntityBootstrap();

            if (autoInitializeOnAwake)
            {
                InitializeAsync(_destroyCts.Token).Forget(HandleUnhandledException);
            }
        }

        private void OnDestroy()
        {
            Shutdown();
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            _destroyCts = null;
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            return InitializeAsync(options, ct);
        }

        public UniTask InitializeAsync(FrameworkOptions initOptions, CancellationToken ct = default)
        {
            if (IsReady)
            {
                return UniTask.CompletedTask;
            }

            if (_shuttingDown)
            {
                return UniTask.FromException(new FrameworkException("Framework 正在关闭，不能开始初始化。"));
            }

            if (_initializing)
            {
                return AttachCallerCancellation(_initializeCompletion.Task, ct);
            }

            _shutdownCompleted = false;
            _shutdownCompletion = null;
            _initializing = true;
            _initializeCompletion = new UniTaskCompletionSource();
            _initializeCts = CreateLinkedCancellationTokenSource(ct);
            InitializeCoreAsync(initOptions, _initializeCts.Token, _initializeCompletion).Forget(HandleUnhandledException);
            return _initializeCompletion.Task;
        }

        private async UniTask InitializeCoreAsync(
            FrameworkOptions initOptions,
            CancellationToken ct,
            UniTaskCompletionSource completion)
        {
            try
            {
                options = initOptions != null ? initOptions.Clone() : FrameworkOptions.CreateDefault();
                ApplyPersistence(options.DontDestroyOnLoad);
                EnsureRuntimeRoot();
                _scene = CreateScene();
                await _scene.InitializeAsync(options, _runtimeRoot, extensions, ct);
                ct.ThrowIfCancellationRequested();
                IsReady = true;
                GameEntry.Register(this);
                completion.TrySetResult();
            }
            catch (System.OperationCanceledException ex)
            {
                CleanupFailedInitialization();
                completion.TrySetCanceled(ex.CancellationToken);
            }
            catch (System.Exception ex)
            {
                CleanupFailedInitialization();
                completion.TrySetException(ex);
            }
            finally
            {
                _initializing = false;
                _initializeCts?.Dispose();
                _initializeCts = null;
            }
        }

        public void Shutdown()
        {
            if (_shuttingDown || _shutdownCompleted)
            {
                return;
            }

            ShutdownAsync().Forget(HandleUnhandledException);
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            if (_shutdownCompleted)
            {
                return UniTask.CompletedTask;
            }

            if (_shuttingDown)
            {
                return AttachCallerCancellation(_shutdownCompletion.Task, ct);
            }

            _shuttingDown = true;
            _shutdownCompletion = new UniTaskCompletionSource();
            ShutdownCoreAsync(ct, _shutdownCompletion).Forget(HandleUnhandledException);
            return _shutdownCompletion.Task;
        }

        private async UniTask ShutdownCoreAsync(CancellationToken ct, UniTaskCompletionSource completion)
        {
            System.Exception shutdownError = null;
            _initializeCts?.Cancel();
            if (_initializing && _initializeCompletion != null)
            {
                try
                {
                    await _initializeCompletion.Task;
                }
                catch (System.Exception)
                {
                    // 初始化失败或取消后仍需继续清理已创建的运行时对象。
                }
            }

            IsReady = false;
            GameEntry.Unregister(this);

            var scene = _scene;
            _scene = null;
            try
            {
                if (scene != null)
                {
                    await scene.ShutdownAsync(ct);
                    if (!scene.IsDestroyed)
                    {
                        scene.Destroy();
                    }
                }
            }
            catch (System.Exception ex)
            {
                shutdownError = ex;
                if (scene != null && !scene.IsDestroyed)
                {
                    scene.Destroy();
                }
            }
            finally
            {
                if (_runtimeRoot != null)
                {
                    Destroy(_runtimeRoot.gameObject);
                }

                _runtimeRoot = null;
                _shuttingDown = false;
                _shutdownCompleted = true;
            }

            if (shutdownError is System.OperationCanceledException canceled)
            {
                completion.TrySetCanceled(canceled.CancellationToken);
            }
            else if (shutdownError != null)
            {
                completion.TrySetException(shutdownError);
            }
            else
            {
                completion.TrySetResult();
            }
        }

        private void CleanupFailedInitialization()
        {
            IsReady = false;
            GameEntry.Unregister(this);
            if (_scene != null && !_scene.IsDestroyed)
            {
                _scene.Destroy();
            }

            _scene = null;
        }

        private CancellationTokenSource CreateLinkedCancellationTokenSource(CancellationToken callerToken)
        {
            var destroyToken = _destroyCts != null ? _destroyCts.Token : default;
            return CancellationTokenSource.CreateLinkedTokenSource(callerToken, destroyToken);
        }

        private static UniTask AttachCallerCancellation(UniTask task, CancellationToken ct)
        {
            return ct.CanBeCanceled ? task.AttachExternalCancellation(ct) : task;
        }

        private void ApplyPersistence(bool persistent)
        {
            if (persistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void EnsureRuntimeRoot()
        {
            if (_runtimeRoot != null)
            {
                return;
            }

            var root = new GameObject("[GameEntity.Unity.Framework.Runtime]");
            root.transform.SetParent(transform, false);
            _runtimeRoot = root.transform;
        }

        private static FrameworkScene CreateScene()
        {
            var existingScene = World.Instance.GetScene(FrameworkSceneName);
            if (existingScene != null)
            {
                throw new FrameworkException($"Framework Scene 已存在：{FrameworkSceneName}");
            }

            return (FrameworkScene)World.Instance.AddScene(
                FrameworkSceneName,
                new FrameworkScene(FrameworkSceneName));
        }

        private void EnsureGameEntityBootstrap()
        {
#if UNITY_2023_1_OR_NEWER
            var bootstrap = FindFirstObjectByType<GameEntityRunner>();
#else
            var bootstrap = FindObjectOfType<GameEntityRunner>();
#endif
            if (bootstrap != null)
            {
                return;
            }

            gameObject.AddComponent<GameEntityRunner>();
        }

        private static void HandleUnhandledException(System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
