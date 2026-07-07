using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    [Serializable]
    public readonly struct FrameworkInputButton
    {
        public readonly bool IsDown;
        public readonly bool PressedThisFrame;
        public readonly bool ReleasedThisFrame;

        public FrameworkInputButton(bool isDown, bool pressedThisFrame, bool releasedThisFrame)
        {
            IsDown = isDown;
            PressedThisFrame = pressedThisFrame;
            ReleasedThisFrame = releasedThisFrame;
        }

        public static FrameworkInputButton Up => new FrameworkInputButton(false, false, false);
    }

}
