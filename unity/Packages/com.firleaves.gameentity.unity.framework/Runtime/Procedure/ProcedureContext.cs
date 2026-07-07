using System;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
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
