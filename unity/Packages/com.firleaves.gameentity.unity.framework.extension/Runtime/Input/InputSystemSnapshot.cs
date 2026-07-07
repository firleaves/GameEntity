using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    public sealed class InputSystemSnapshot
    {
        public int CurrentFrame;
        public FrameworkInputSourceKind SourceKind;
        public FrameworkInputFrame LatestFrame;
        public IReadOnlyList<FrameworkInputFrame> History;
    }

}
