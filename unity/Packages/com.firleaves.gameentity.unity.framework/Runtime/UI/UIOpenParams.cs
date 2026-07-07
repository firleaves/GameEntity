using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class UIOpenParams
    {
        public AssetKey ViewKey;
        public string Group = "Default";
        public int Depth;
        public UIReusePolicy ReusePolicy = UIReusePolicy.Single;
        public object UserData;
        public Transform ParentOverride;
    }

}
