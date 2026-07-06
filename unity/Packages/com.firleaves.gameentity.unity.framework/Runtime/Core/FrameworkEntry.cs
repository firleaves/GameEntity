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

        private FrameworkScene _scene;
        private Transform _runtimeRoot;
        private CancellationTokenSource _destroyCts;
        private bool _initializing;
        private bool _shutdown;

        public FrameworkScene Scene => _scene;
        public bool IsReady { get; private set; }
        public FrameworkOptions Options => options;

        private const string FrameworkSceneName = "GameEntity.Unity.Framework";

        private void Awake()
        {
            _destroyCts = new CancellationTokenSource();
            EnsureGameEntityBootstrap();
            if (options != null && options.DontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

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

        public async UniTask InitializeAsync(FrameworkOptions initOptions, CancellationToken ct = default)
        {
            if (IsReady || _initializing)
            {
                return;
            }

            _initializing = true;
            _shutdown = false;
            try
            {
                options = initOptions != null ? initOptions.Clone() : FrameworkOptions.CreateDefault();
                EnsureRuntimeRoot();
                _scene = CreateScene();
                await _scene.InitializeAsync(options, _runtimeRoot, ct);
                IsReady = true;
                GameEntry.Register(this);
            }
            catch
            {
                if (_scene != null && !_scene.IsDestroyed)
                {
                    _scene.Destroy();
                }

                _scene = null;
                IsReady = false;
                throw;
            }
            finally
            {
                _initializing = false;
            }
        }

        public void Shutdown()
        {
            if (_shutdown)
            {
                return;
            }

            _shutdown = true;
            ShutdownAsync().Forget(HandleUnhandledException);
        }

        public async UniTask ShutdownAsync(CancellationToken ct = default)
        {
            IsReady = false;
            GameEntry.Unregister(this);

            if (_scene != null)
            {
                await _scene.ShutdownAsync(ct);
                if (!_scene.IsDestroyed)
                {
                    _scene.Destroy();
                }

                _scene = null;
            }

            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot.gameObject);
                _runtimeRoot = null;
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
