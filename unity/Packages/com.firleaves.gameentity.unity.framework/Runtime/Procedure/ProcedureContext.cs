using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public readonly struct ProcedureContext
    {
        public readonly string StateName;
        public readonly IProcedureSystem ProcedureSystem;
        public readonly CancellationToken CancellationToken;

        public ProcedureContext(
            string stateName,
            IProcedureSystem procedureSystem,
            CancellationToken cancellationToken = default)
        {
            StateName = stateName;
            ProcedureSystem = procedureSystem;
            CancellationToken = cancellationToken;
        }
    }

}
