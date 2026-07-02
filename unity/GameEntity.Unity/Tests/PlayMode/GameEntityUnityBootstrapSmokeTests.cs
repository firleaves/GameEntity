using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameEntity.Unity.Tests
{
    public sealed class GameEntityUnityBootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_DrivesRuntime_AndMirrorsEntityTree()
        {
            GameObject root = new GameObject("GameEntity.Unity.TestRoot");
            GameEntityUnityBootstrap bootstrap = root.AddComponent<GameEntityUnityBootstrap>();

            yield return null;

            var scene = new UnitySmokeScene();
            var entity = scene.AddChild<UnitySmokeEntity>();
            var component = entity.AddComponent<UnitySmokeComponent>();

            yield return null;

            Assert.NotNull(bootstrap.Registry);
            Assert.NotNull(bootstrap.Registry.GetView(scene));
            Assert.NotNull(bootstrap.Registry.GetView(entity));
            Assert.NotNull(bootstrap.Registry.GetView(component));
            Assert.AreSame(bootstrap.Registry.GetView(scene).transform, bootstrap.Registry.GetView(entity).transform.parent);
            Assert.AreSame(bootstrap.Registry.GetView(entity).transform, bootstrap.Registry.GetView(component).transform.parent);
            Assert.Greater(entity.UpdateCount, 0);

            entity.Dispose();
            yield return null;

            Assert.IsNull(bootstrap.Registry.GetView(entity));
            Object.Destroy(root);
        }

        private sealed class UnitySmokeScene : Scene
        {
            public UnitySmokeScene() : base("UnitySmokeScene")
            {
            }
        }

        private sealed class UnitySmokeEntity : Entity, IAwake, IUpdate
        {
            public int UpdateCount { get; private set; }

            public void Awake()
            {
            }

            public void Update(float deltaTime)
            {
                UpdateCount++;
            }
        }

        private sealed class UnitySmokeComponent : Entity, IAwake
        {
            public void Awake()
            {
            }
        }
    }
}
