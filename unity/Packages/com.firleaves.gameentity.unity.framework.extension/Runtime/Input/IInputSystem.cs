using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    public interface IInputSystem
    {
        int CurrentFrame { get; }
        FrameworkInputFrame LatestFrame { get; }
        FrameworkInputSourceKind SourceKind { get; }
        IReadOnlyList<FrameworkInputFrame> History { get; }

        void SetSource(IFrameworkInputSource source);
        void PushFrame(FrameworkInputFrame frame);
        bool TryConsumeLatest(out FrameworkInputFrame frame);
        InputSystemSnapshot GetSnapshot();
    }

}
