using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public readonly struct UIOpenContext
    {
        public readonly UIOpenParams Params;

        public UIOpenContext(UIOpenParams openParams)
        {
            Params = openParams;
        }
    }

}
