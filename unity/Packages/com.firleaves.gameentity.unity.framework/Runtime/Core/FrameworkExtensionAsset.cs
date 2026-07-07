using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public abstract class FrameworkExtensionAsset : ScriptableObject
    {
        [SerializeField]
        private bool enabled = true;

        public bool Enabled => enabled;

        internal void InstallIfEnabled(FrameworkExtensionContext context)
        {
            if (!enabled)
            {
                return;
            }

            Install(context);
        }

        protected abstract void Install(FrameworkExtensionContext context);
    }
}
