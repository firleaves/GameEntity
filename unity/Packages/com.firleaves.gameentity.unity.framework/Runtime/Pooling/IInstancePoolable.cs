using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IInstancePoolable
    {
        void OnRent(InstanceRentContext context);
        void OnReturn();
        bool CanReleaseFromPool();
    }

}
