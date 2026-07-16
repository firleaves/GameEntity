using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class UIOptions
    {
        public Transform UIRoot;
        public string DefaultGroup = "Default";

        public UIOptions Clone()
        {
            return new UIOptions
            {
                UIRoot = UIRoot,
                DefaultGroup = DefaultGroup
            };
        }

        public static UIOptions CreateDefault()
        {
            return new UIOptions();
        }
    }

}
