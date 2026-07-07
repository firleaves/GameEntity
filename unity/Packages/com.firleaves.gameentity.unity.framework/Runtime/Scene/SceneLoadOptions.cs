using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEntity.Unity.Framework
{
    public sealed class SceneLoadOptions
    {
        public string PackageName;
        public LoadSceneMode LoadMode = LoadSceneMode.Single;
        public LocalPhysicsMode PhysicsMode = LocalPhysicsMode.None;
        public bool SuspendLoad;
        public bool ActivateOnLoad = true;
        public uint Priority;
    }

}
