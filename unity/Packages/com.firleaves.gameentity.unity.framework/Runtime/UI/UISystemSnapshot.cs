using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class UISystemSnapshot
    {
        public DateTime CapturedAtUtc;
        public IReadOnlyList<UIInfo> OpenUIs;
    }

}
