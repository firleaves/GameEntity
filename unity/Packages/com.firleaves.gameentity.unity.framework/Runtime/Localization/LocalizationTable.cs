using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class LocalizationTable
    {
        public LocalizationEntry[] Entries;
    }

}
