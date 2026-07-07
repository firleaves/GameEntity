using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public readonly struct AudioPlayHandle : IEquatable<AudioPlayHandle>
    {
        internal readonly int Id;

        internal AudioPlayHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id > 0;

        public bool Equals(AudioPlayHandle other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioPlayHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

}
