using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class ProcedureSystemEntity : Entity, IAwake, IUpdate, IDestroy, IProcedureSystem
    {
        private readonly Dictionary<string, Func<IProcedure>> _factories = new Dictionary<string, Func<IProcedure>>(StringComparer.Ordinal);
        private readonly Dictionary<string, IProcedure> _stateCache = new Dictionary<string, IProcedure>(StringComparer.Ordinal);
        private readonly Queue<TransitionRequest> _requests = new Queue<TransitionRequest>();
        private IProcedure _currentState;
        private string _currentStateName;
        private bool _processingRequests;
        private CancellationTokenSource _lifetimeCts;

        public IProcedure CurrentState => _currentState;
        public string CurrentStateName => _currentStateName;
        public bool IsTransitioning => _processingRequests;

        public void Awake()
        {
            _lifetimeCts?.Dispose();
            _lifetimeCts = new CancellationTokenSource();
            _requests.Clear();
            _currentState = null;
            _currentStateName = null;
            _processingRequests = false;
        }

        public void Update(float deltaTime)
        {
            if (_processingRequests)
            {
                return;
            }

            _currentState?.Update(deltaTime);
        }

        public void OnDestroy()
        {
            var cancellationToken = _lifetimeCts != null ? _lifetimeCts.Token : default;
            _lifetimeCts?.Cancel();
            CancelPendingRequests(cancellationToken);
            _factories.Clear();
            _stateCache.Clear();
            _currentState = null;
            _currentStateName = null;
            _processingRequests = false;
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        public void Register<TState>(string stateName = null) where TState : IProcedure, new()
        {
            Register(string.IsNullOrWhiteSpace(stateName) ? typeof(TState).Name : stateName, () => new TState());
        }

        public void Register(string stateName, Func<IProcedure> factory)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new FrameworkException("注册 Procedure 失败：stateName 不能为空。");
            }

            _factories[stateName] = factory ?? throw new FrameworkException($"注册 Procedure 失败：{stateName} 的 factory 不能为空。");
        }

        public bool HasState(string stateName)
        {
            return !string.IsNullOrWhiteSpace(stateName) && _factories.ContainsKey(stateName);
        }

        public UniTask ChangeStateAsync<TState>() where TState : IProcedure, new()
        {
            var stateName = typeof(TState).Name;
            if (!HasState(stateName))
            {
                Register<TState>(stateName);
            }

            return ChangeStateAsync(stateName);
        }

        public UniTask ChangeStateAsync(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new FrameworkException("切换流程失败：stateName 不能为空。");
            }

            if (!_factories.ContainsKey(stateName))
            {
                throw new FrameworkException($"切换流程失败：未注册状态 {stateName}。");
            }

            if (!_processingRequests && string.Equals(_currentStateName, stateName, StringComparison.Ordinal))
            {
                return UniTask.CompletedTask;
            }

            return EnqueueRequest(stateName, stop: false);
        }

        public UniTask StopAsync()
        {
            if (!_processingRequests && _currentState == null)
            {
                _currentStateName = null;
                return UniTask.CompletedTask;
            }

            return EnqueueRequest(null, stop: true);
        }

        private UniTask EnqueueRequest(string stateName, bool stop)
        {
            if (_lifetimeCts == null || _lifetimeCts.IsCancellationRequested || IsDestroyed)
            {
                return UniTask.FromException(new FrameworkException("ProcedureSystem 已销毁，不能提交状态转换。"));
            }

            var request = new TransitionRequest(stateName, stop);
            _requests.Enqueue(request);
            if (!_processingRequests)
            {
                ProcessRequestsAsync(_lifetimeCts.Token).Forget(Debug.LogException);
            }

            return request.Completion.Task;
        }

        private async UniTask ProcessRequestsAsync(CancellationToken ct)
        {
            _processingRequests = true;
            try
            {
                while (_requests.Count > 0)
                {
                    if (ct.IsCancellationRequested)
                    {
                        CancelPendingRequests(ct);
                        break;
                    }

                    var request = _requests.Dequeue();
                    try
                    {
                        if (request.Stop)
                        {
                            await ExecuteStopAsync(ct);
                        }
                        else
                        {
                            await ExecuteChangeStateAsync(request.StateName, ct);
                        }

                        request.Completion.TrySetResult();
                    }
                    catch (OperationCanceledException ex)
                    {
                        request.Completion.TrySetCanceled(ex.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        request.Completion.TrySetException(ex);
                    }
                }
            }
            finally
            {
                _processingRequests = false;
                if (ct.IsCancellationRequested)
                {
                    CancelPendingRequests(ct);
                }
            }
        }

        private async UniTask ExecuteChangeStateAsync(string stateName, CancellationToken ct)
        {
            if (string.Equals(_currentStateName, stateName, StringComparison.Ordinal))
            {
                return;
            }

            if (_currentState != null)
            {
                await _currentState
                    .ExitAsync(new ProcedureContext(_currentStateName, this, ct))
                    .AttachExternalCancellation(ct);
            }

            ct.ThrowIfCancellationRequested();
            var nextState = GetOrCreateState(stateName);
            _currentState = nextState;
            _currentStateName = stateName;
            await nextState
                .EnterAsync(new ProcedureContext(stateName, this, ct))
                .AttachExternalCancellation(ct);
        }

        private async UniTask ExecuteStopAsync(CancellationToken ct)
        {
            var currentState = _currentState;
            var currentStateName = _currentStateName;
            try
            {
                if (currentState != null)
                {
                    await currentState
                        .ExitAsync(new ProcedureContext(currentStateName, this, ct))
                        .AttachExternalCancellation(ct);
                }
            }
            finally
            {
                _currentState = null;
                _currentStateName = null;
            }
        }

        private void CancelPendingRequests(CancellationToken ct)
        {
            while (_requests.Count > 0)
            {
                _requests.Dequeue().Completion.TrySetCanceled(ct);
            }
        }

        private IProcedure GetOrCreateState(string stateName)
        {
            if (_stateCache.TryGetValue(stateName, out var state))
            {
                return state;
            }

            state = _factories[stateName]();
            if (state == null)
            {
                throw new FrameworkException($"创建流程状态失败：{stateName} 的 factory 返回 null。");
            }

            _stateCache.Add(stateName, state);
            return state;
        }

        private sealed class TransitionRequest
        {
            public TransitionRequest(string stateName, bool stop)
            {
                StateName = stateName;
                Stop = stop;
                Completion = new UniTaskCompletionSource();
            }

            public string StateName { get; }
            public bool Stop { get; }
            public UniTaskCompletionSource Completion { get; }
        }
    }

}
