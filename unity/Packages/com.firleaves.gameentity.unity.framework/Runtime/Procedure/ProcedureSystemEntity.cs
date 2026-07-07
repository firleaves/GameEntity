using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class ProcedureSystemEntity : Entity, IAwake, IUpdate, IDestroy, IProcedureSystem
    {
        private readonly Dictionary<string, Func<IProcedure>> _factories = new Dictionary<string, Func<IProcedure>>(StringComparer.Ordinal);
        private readonly Dictionary<string, IProcedure> _stateCache = new Dictionary<string, IProcedure>(StringComparer.Ordinal);
        private IProcedure _currentState;
        private string _currentStateName;
        private bool _isTransitioning;
        private string _pendingStateName;

        public IProcedure CurrentState => _currentState;
        public string CurrentStateName => _currentStateName;
        public bool IsTransitioning => _isTransitioning;

        public void Awake()
        {
        }

        public void Update(float deltaTime)
        {
            if (_isTransitioning)
            {
                return;
            }

            _currentState?.Update(deltaTime);
        }

        public void OnDestroy()
        {
            StopAsync().Forget(Debug.LogException);
            _factories.Clear();
            _stateCache.Clear();
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

        public async UniTask ChangeStateAsync(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new FrameworkException("切换流程失败：stateName 不能为空。");
            }

            if (!_factories.ContainsKey(stateName))
            {
                throw new FrameworkException($"切换流程失败：未注册状态 {stateName}。");
            }

            if (string.Equals(_currentStateName, stateName, StringComparison.Ordinal))
            {
                return;
            }

            if (_isTransitioning)
            {
                _pendingStateName = stateName;
                return;
            }

            _isTransitioning = true;
            try
            {
                if (_currentState != null)
                {
                    await _currentState.ExitAsync(new ProcedureContext(_currentStateName, this));
                }

                var nextState = GetOrCreateState(stateName);
                _currentState = nextState;
                _currentStateName = stateName;
                await _currentState.EnterAsync(new ProcedureContext(stateName, this));
            }
            finally
            {
                _isTransitioning = false;
            }

            if (!string.IsNullOrWhiteSpace(_pendingStateName))
            {
                var pending = _pendingStateName;
                _pendingStateName = null;
                await ChangeStateAsync(pending);
            }
        }

        public async UniTask StopAsync()
        {
            if (_currentState == null)
            {
                _currentStateName = null;
                _pendingStateName = null;
                return;
            }

            if (_isTransitioning)
            {
                _pendingStateName = null;
                return;
            }

            _isTransitioning = true;
            try
            {
                await _currentState.ExitAsync(new ProcedureContext(_currentStateName, this));
                _currentState = null;
                _currentStateName = null;
                _pendingStateName = null;
            }
            finally
            {
                _isTransitioning = false;
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
    }

}
