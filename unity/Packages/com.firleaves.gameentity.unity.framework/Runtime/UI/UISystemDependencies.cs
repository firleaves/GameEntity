using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameEntity.Unity.Framework
{
    public sealed class UISystemDependencies
    {
        public UIOptions Options;
        public IInstancePool InstancePool;
        public Transform FrameworkRoot;
        public bool AutoCreateEventSystem;
    }

}
