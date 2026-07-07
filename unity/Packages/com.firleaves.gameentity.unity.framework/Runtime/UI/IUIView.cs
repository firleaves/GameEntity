using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IUIView
    {
        GameObject GameObject { get; }
        Transform Transform { get; }
        Canvas Canvas { get; }
        void SetVisible(bool visible);
        void SetDepth(int depth);
    }

}
