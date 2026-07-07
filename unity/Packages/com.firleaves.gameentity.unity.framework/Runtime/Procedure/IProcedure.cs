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

}
