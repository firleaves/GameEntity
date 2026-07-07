using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class FrameworkExtensionContext
    {
        internal FrameworkExtensionContext(FrameworkScene scene, FrameworkOptions options, Transform frameworkRoot)
        {
            Scene = scene ?? throw new FrameworkException("FrameworkExtensionContext 初始化失败：Scene 不能为空。");
            Options = options ?? FrameworkOptions.CreateDefault();
            FrameworkRoot = frameworkRoot;
        }

        public FrameworkScene Scene { get; }
        public FrameworkOptions Options { get; }
        public Transform FrameworkRoot { get; }

        public void SetService<T>(T service)
        {
            Scene.SetService(service);
        }

        public bool TryGetService<T>(out T service)
        {
            return Scene.TryGetService(out service);
        }

        public T GetRequiredService<T>()
        {
            return Scene.GetRequiredService<T>();
        }

        public Transform CreateChildRoot(string name)
        {
            var root = new GameObject(name);
            if (FrameworkRoot != null)
            {
                root.transform.SetParent(FrameworkRoot, false);
            }
            else
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            root.SetActive(false);
            return root.transform;
        }
    }
}
