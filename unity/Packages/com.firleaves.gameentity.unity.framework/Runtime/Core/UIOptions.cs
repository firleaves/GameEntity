using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class UIOptions
    {
        public Transform UIRoot;
        public string DefaultGroup = "Default";
        public int GroupDepthStep = 1000;
        public bool AutoCreateCanvas = true;
        public bool UseInstancePool = true;

        public UIOptions Clone()
        {
            return new UIOptions
            {
                UIRoot = UIRoot,
                DefaultGroup = DefaultGroup,
                GroupDepthStep = GroupDepthStep,
                AutoCreateCanvas = AutoCreateCanvas,
                UseInstancePool = UseInstancePool
            };
        }

        public static UIOptions CreateDefault()
        {
            return new UIOptions();
        }
    }

}
