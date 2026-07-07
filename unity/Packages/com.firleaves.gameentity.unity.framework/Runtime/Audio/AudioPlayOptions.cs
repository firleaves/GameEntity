using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AudioPlayOptions
    {
        public AudioChannel Channel = AudioChannel.Sfx;
        public float Volume = 1f;
        public bool Loop;
        public bool IgnoreMute;
        public Transform Parent;
        public Vector3? Position;
    }

}
