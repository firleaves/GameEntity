using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public abstract class Procedure : IProcedure
    {
        public virtual UniTask EnterAsync(ProcedureContext context)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask ExitAsync(ProcedureContext context)
        {
            return UniTask.CompletedTask;
        }

        public virtual void Update(float deltaTime)
        {
        }
    }

}
