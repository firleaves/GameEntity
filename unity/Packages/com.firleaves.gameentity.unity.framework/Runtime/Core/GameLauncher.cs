using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity.Unity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-8000)]
    public sealed class GameLauncher : MonoBehaviour
    {
        [SerializeField]
        private FrameworkEntry framework;

        [SerializeField]
        private bool initializeOnStart = true;

        [SerializeField]
        private bool includeInactiveTasks;

        [SerializeField]
        private bool autoCreateGameEntityBootstrap = true;

        private readonly List<IGameLaunchTask> _tasks = new List<IGameLaunchTask>(8);
        private CancellationTokenSource _destroyCts;
        private UniTaskCompletionSource _launchCompletion;
        private bool _launching;

        public bool IsLaunched { get; private set; }

        private void Awake()
        {
            _destroyCts = new CancellationTokenSource();
            if (autoCreateGameEntityBootstrap)
            {
                EnsureGameEntityBootstrap();
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                LaunchAsync(_destroyCts.Token).Forget(Debug.LogException);
            }
        }

        private void OnDestroy()
        {
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            _destroyCts = null;
        }

        public UniTask LaunchAsync(CancellationToken ct = default)
        {
            if (IsLaunched)
            {
                return UniTask.CompletedTask;
            }

            if (_launching)
            {
                return ct.CanBeCanceled
                    ? _launchCompletion.Task.AttachExternalCancellation(ct)
                    : _launchCompletion.Task;
            }

            _launching = true;
            _launchCompletion = new UniTaskCompletionSource();
            LaunchCoreAsync(ct, _launchCompletion).Forget(Debug.LogException);
            return _launchCompletion.Task;
        }

        private async UniTask LaunchCoreAsync(CancellationToken ct, UniTaskCompletionSource completion)
        {
            try
            {
                var targetFramework = ResolveFramework();
                if (!targetFramework.IsReady)
                {
                    await targetFramework.InitializeAsync(ct);
                }

                CollectTasks();
                for (var i = 0; i < _tasks.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    await _tasks[i].LaunchAsync(targetFramework, ct);
                }

                IsLaunched = true;
                completion.TrySetResult();
            }
            catch (OperationCanceledException ex)
            {
                completion.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                _launching = false;
            }
        }

        private FrameworkEntry ResolveFramework()
        {
            if (framework != null)
            {
                return framework;
            }

            framework = GetComponent<FrameworkEntry>();
            if (framework != null)
            {
                return framework;
            }

#if UNITY_2023_1_OR_NEWER
            framework = FindFirstObjectByType<FrameworkEntry>();
#else
            framework = FindObjectOfType<FrameworkEntry>();
#endif
            if (framework != null)
            {
                return framework;
            }

            framework = gameObject.AddComponent<FrameworkEntry>();
            return framework;
        }

        private void CollectTasks()
        {
            _tasks.Clear();
#if UNITY_2023_1_OR_NEWER
            var behaviours = includeInactiveTasks
                ? FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                : FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var behaviours = includeInactiveTasks
                ? FindObjectsOfType<MonoBehaviour>(includeInactive: true)
                : FindObjectsOfType<MonoBehaviour>();
#endif
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IGameLaunchTask task)
                {
                    _tasks.Add(task);
                }
            }

            _tasks.Sort(CompareTaskOrder);
        }

        private static int CompareTaskOrder(IGameLaunchTask a, IGameLaunchTask b)
        {
            var order = a.Order.CompareTo(b.Order);
            return order != 0 ? order : string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName);
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
    }
}
