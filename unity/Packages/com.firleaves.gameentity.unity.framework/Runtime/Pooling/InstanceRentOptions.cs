using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class InstanceRentOptions
    {
        public bool SetActive = true;
        public bool WorldPositionStays;
        public Vector3? Position;
        public Quaternion? Rotation;
        public PoolPolicy PolicyOverride;
    }

}
