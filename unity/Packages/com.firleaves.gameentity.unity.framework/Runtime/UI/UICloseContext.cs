using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public readonly struct UICloseContext
    {
        public readonly UICloseReason Reason;

        public UICloseContext(UICloseReason reason)
        {
            Reason = reason;
        }
    }

}
