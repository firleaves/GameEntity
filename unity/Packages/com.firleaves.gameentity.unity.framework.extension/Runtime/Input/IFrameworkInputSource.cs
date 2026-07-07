using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    public interface IFrameworkInputSource
    {
        bool TryReadInput(int frame, float time, out FrameworkInputFrame input);
    }

}
