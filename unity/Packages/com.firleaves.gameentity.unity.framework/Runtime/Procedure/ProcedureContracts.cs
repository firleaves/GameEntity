using System;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface IProcedure
    {
        UniTask EnterAsync(ProcedureContext context);
        UniTask ExitAsync(ProcedureContext context);
        void Update(float deltaTime);
    }

    public interface IProcedureSystem
    {
        IProcedure CurrentState { get; }
        string CurrentStateName { get; }
        bool IsTransitioning { get; }

        void Register<TState>(string stateName = null) where TState : IProcedure, new();
        void Register(string stateName, Func<IProcedure> factory);
        bool HasState(string stateName);
        UniTask ChangeStateAsync<TState>() where TState : IProcedure, new();
        UniTask ChangeStateAsync(string stateName);
        UniTask StopAsync();
    }

    public readonly struct ProcedureContext
    {
        public readonly string StateName;
        public readonly IProcedureSystem ProcedureSystem;

        public ProcedureContext(string stateName, IProcedureSystem procedureSystem)
        {
            StateName = stateName;
            ProcedureSystem = procedureSystem;
        }
    }
}
