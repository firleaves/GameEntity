using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameEntity.Unity.Tests
{
    public sealed class GameEntityRunnerSmokeTests
    {
        [UnityTest]
        public IEnumerator Runner_DrivesRuntime_AndMirrorsEntityTree()
        {
            GameObject root = new GameObject("GameEntity.Unity.TestRoot");
            GameEntityRunner runner = root.AddComponent<GameEntityRunner>();

            yield return null;

            var scene = (UnitySmokeScene)World.Instance.AddScene("UnitySmokeScene", new UnitySmokeScene());
            var entity = scene.AddChild<UnitySmokeEntity>();
            var component = entity.AddComponent<UnitySmokeComponent>();

            yield return null;

            Assert.NotNull(runner.Registry);
            Assert.NotNull(runner.Registry.GetView(scene));
            Assert.NotNull(runner.Registry.GetView(entity));
            Assert.NotNull(runner.Registry.GetView(component));
            Assert.AreSame(runner.Registry.GetView(scene).transform, runner.Registry.GetView(entity).transform.parent);
            Assert.AreSame(runner.Registry.GetView(entity).transform, runner.Registry.GetView(component).transform.parent);
            Assert.Greater(entity.UpdateCount, 0);

            entity.Destroy();
            yield return null;

            Assert.IsNull(runner.Registry.GetView(entity));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Runner_ReplaysExistingEntityTree_WhenCreatedAfterWorldContent()
        {
            var scene = (UnitySmokeScene)World.Instance.AddScene(
                "ExistingWorldScene",
                new UnitySmokeScene("ExistingWorldScene"));
            var entity = scene.AddChild<UnitySmokeEntity>();
            var component = entity.AddComponent<UnitySmokeComponent>();

            GameObject root = new GameObject("GameEntity.Unity.LateRunnerRoot");
            GameEntityRunner runner = root.AddComponent<GameEntityRunner>();

            yield return null;

            ComponentView sceneView = runner.Registry.GetView(scene);
            ComponentView entityView = runner.Registry.GetView(entity);
            ComponentView componentView = runner.Registry.GetView(component);

            Assert.NotNull(sceneView);
            Assert.NotNull(entityView);
            Assert.NotNull(componentView);
            Assert.AreSame(sceneView.transform, entityView.transform.parent);
            Assert.AreSame(entityView.transform, componentView.transform.parent);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Runner_WhenNotOwningWorld_DoesNotDisposeWorldOnDestroy()
        {
            GameObject root = new GameObject("GameEntity.Unity.ExternalWorldRoot");
            GameEntityRunner runner = root.AddComponent<GameEntityRunner>();
            runner.OwnsWorldLifetime = false;

            yield return null;

            var scene = (UnitySmokeScene)World.Instance.AddScene("ExternalWorldScene", new UnitySmokeScene("ExternalWorldScene"));

            Object.Destroy(root);
            yield return null;

            Assert.AreSame(scene, World.Instance.GetScene("ExternalWorldScene"));
            scene.Destroy();
            World.Instance.Dispose();
        }

        [UnityTest]
        public IEnumerator Runner_DrivesFixedUpdateAtConfiguredRate()
        {
            GameObject root = new GameObject("GameEntity.Unity.FixedUpdateRoot");
            GameEntityRunner runner = root.AddComponent<GameEntityRunner>();

            yield return null;

            var scene = (UnitySmokeScene)World.Instance.AddScene("FixedUpdateScene", new UnitySmokeScene("FixedUpdateScene"));
            var entity = scene.AddChild<UnityFixedSmokeEntity>();

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.Greater(entity.FixedUpdateCount, 0);
            Assert.AreEqual(1f / runner.FixedUpdatesPerSecond, entity.LastFixedDeltaTime, 0.00001f);
            Assert.That(runner.FixedInterpolationAlpha, Is.InRange(0f, 1f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Registry_RemovesDestroyedUnityView_WhenQueried()
        {
            GameObject root = new GameObject("GameEntity.Unity.RegistryRoot");
            var registry = new UnityEntityViewRegistry(root.transform, autoCreateViews: false, destroyViewsOnEntityDestroy: true);
            var scene = (UnitySmokeScene)World.Instance.AddScene("DestroyedViewScene", new UnitySmokeScene("DestroyedViewScene"));
            GameObject viewObject = new GameObject("DestroyedView");
            registry.Bind(scene, viewObject);

            Object.Destroy(viewObject);
            yield return null;

            Assert.IsFalse(registry.TryGetView(scene, out ComponentView view));
            Assert.IsNull(view);
            Assert.AreEqual(0, registry.ViewCount);

            scene.Destroy();
            Object.Destroy(root);
            World.Instance.Dispose();
        }

        [UnityTest]
        public IEnumerator Registry_Rebind_ReleasesPreviousView()
        {
            GameObject root = new GameObject("GameEntity.Unity.RebindRoot");
            var registry = new UnityEntityViewRegistry(root.transform, autoCreateViews: false, destroyViewsOnEntityDestroy: true);
            var scene = (UnitySmokeScene)World.Instance.AddScene("RebindScene", new UnitySmokeScene("RebindScene"));
            GameObject firstObject = new GameObject("FirstView");
            GameObject secondObject = new GameObject("SecondView");

            ComponentView firstView = registry.Bind(scene, firstObject);
            ComponentView secondView = registry.Bind(scene, secondObject);

            Assert.AreSame(secondView, registry.GetView(scene));
            Assert.IsTrue(firstView.IsReleased);

            yield return null;

            Assert.IsTrue(firstObject == null);
            Assert.IsFalse(secondObject == null);

            scene.Destroy();
            Object.Destroy(root);
            World.Instance.Dispose();
        }

        private sealed class UnitySmokeScene : Scene
        {
            public UnitySmokeScene() : base("UnitySmokeScene")
            {
            }

            public UnitySmokeScene(string name) : base(name)
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

        private sealed class UnityFixedSmokeEntity : Entity, IAwake, IFixedUpdate
        {
            public int FixedUpdateCount { get; private set; }

            public float LastFixedDeltaTime { get; private set; }

            public void Awake()
            {
            }

            public void FixedUpdate(float fixedDeltaTime)
            {
                FixedUpdateCount++;
                LastFixedDeltaTime = fixedDeltaTime;
            }
        }
    }
}
