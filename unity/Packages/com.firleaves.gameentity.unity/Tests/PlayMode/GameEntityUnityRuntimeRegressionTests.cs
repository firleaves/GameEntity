using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameEntity.Unity.Tests
{
    public sealed class GameEntityUnityRuntimeRegressionTests
    {
        private readonly List<Scene> _scenes = new List<Scene>();

        private GameObject _root;
        private GameEntityRunner _runner;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = new GameObject("GameEntity.Unity.RegressionRoot");
            _runner = _root.AddComponent<GameEntityRunner>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (Scene scene in _scenes)
            {
                if (scene != null && !scene.IsDestroyed)
                {
                    scene.Destroy();
                }
            }

            _scenes.Clear();

            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            _runner = null;

            yield return null;
        }

        [UnityTest]
        public IEnumerator EntityTreeAndComponents_ShouldMaintainCoreRelationsAndUnityViews()
        {
            var scene = Track(new RegressionScene("TreeScene"));
            var parent1 = scene.AddChild<TreeProbeEntity>();
            var parent2 = scene.AddChild<TreeProbeEntity>();
            var child = parent1.AddChild<TreeProbeEntity>();
            var component = child.AddComponent<TreeProbeComponent>();

            yield return null;

            Assert.AreNotEqual(parent1.Id, parent2.Id);
            Assert.AreNotEqual(parent1.InstanceId, parent2.InstanceId);
            Assert.IsTrue(parent1.ContainsChild(child.Id));
            Assert.AreSame(parent1, child.Parent);
            Assert.AreSame(component, child.GetComponent<TreeProbeComponent>());
            Assert.AreSame(child, component.Parent);
            Assert.AreEqual(child.Id, component.Id);

            ComponentView parent1View = _runner.Registry.GetView(parent1);
            ComponentView parent2View = _runner.Registry.GetView(parent2);
            ComponentView childView = _runner.Registry.GetView(child);
            ComponentView componentView = _runner.Registry.GetView(component);

            Assert.NotNull(parent1View);
            Assert.NotNull(parent2View);
            Assert.NotNull(childView);
            Assert.NotNull(componentView);
            Assert.AreSame(parent1View.transform, childView.transform.parent);
            Assert.AreSame(childView.transform, componentView.transform.parent);

            child.ReparentTo(parent2);

            yield return null;

            Assert.IsFalse(parent1.ContainsChild(child.Id));
            Assert.IsTrue(parent2.ContainsChild(child.Id));
            Assert.AreSame(parent2, child.Parent);
            Assert.AreSame(parent2View.transform, childView.transform.parent);

            child.Destroy();

            yield return null;

            Assert.IsFalse(parent2.ContainsChild(child.Id));
            Assert.IsTrue(child.IsDestroyed);
            Assert.IsTrue(component.IsDestroyed);
            Assert.IsNull(_runner.Registry.GetView(child));
            Assert.IsNull(_runner.Registry.GetView(component));
        }

        [UnityTest]
        public IEnumerator RemoveComponent_ShouldDisposeComponentOnlyAndKeepEntityView()
        {
            var scene = Track(new RegressionScene("ComponentScene"));
            var entity = scene.AddChild<TreeProbeEntity>();
            var component = entity.AddComponent<TreeProbeComponent>();

            yield return null;

            ComponentView entityView = _runner.Registry.GetView(entity);
            ComponentView componentView = _runner.Registry.GetView(component);

            Assert.NotNull(entityView);
            Assert.NotNull(componentView);
            Assert.AreSame(entityView.transform, componentView.transform.parent);

            entity.RemoveComponent<TreeProbeComponent>();

            yield return null;

            Assert.IsNull(entity.GetComponent<TreeProbeComponent>());
            Assert.AreEqual(0, entity.ComponentsCount());
            Assert.IsFalse(entity.IsDestroyed);
            Assert.IsTrue(scene.ContainsChild(entity.Id));
            Assert.IsTrue(component.IsDestroyed);
            Assert.AreEqual(1, component.DestroyCount);
            Assert.NotNull(_runner.Registry.GetView(entity));
            Assert.IsNull(_runner.Registry.GetView(component));
        }

        [UnityTest]
        public IEnumerator SceneDispose_ShouldDisposeEntireEntityTreeAndViews()
        {
            var scene = Track(new RegressionScene("DisposeScene"));
            var parent = scene.AddChild<TreeProbeEntity>();
            var child = parent.AddChild<TreeProbeEntity>();
            var grandchild = child.AddChild<TreeProbeEntity>();
            var parentComponent = parent.AddComponent<TreeProbeComponent>();
            var childComponent = child.AddComponent<TreeProbeComponent>();

            yield return null;

            Assert.NotNull(_runner.Registry.GetView(scene));
            Assert.NotNull(_runner.Registry.GetView(parent));
            Assert.NotNull(_runner.Registry.GetView(child));
            Assert.NotNull(_runner.Registry.GetView(grandchild));
            Assert.NotNull(_runner.Registry.GetView(parentComponent));
            Assert.NotNull(_runner.Registry.GetView(childComponent));

            scene.Destroy();

            yield return null;

            Assert.IsTrue(scene.IsDestroyed);
            Assert.IsTrue(parent.IsDestroyed);
            Assert.IsTrue(child.IsDestroyed);
            Assert.IsTrue(grandchild.IsDestroyed);
            Assert.IsTrue(parentComponent.IsDestroyed);
            Assert.IsTrue(childComponent.IsDestroyed);
            Assert.AreEqual(1, parent.DestroyCount);
            Assert.AreEqual(1, child.DestroyCount);
            Assert.AreEqual(1, grandchild.DestroyCount);
            Assert.AreEqual(1, parentComponent.DestroyCount);
            Assert.AreEqual(1, childComponent.DestroyCount);
            Assert.IsNull(_runner.Registry.GetView(scene));
            Assert.IsNull(_runner.Registry.GetView(parent));
            Assert.IsNull(_runner.Registry.GetView(child));
            Assert.IsNull(_runner.Registry.GetView(grandchild));
            Assert.IsNull(_runner.Registry.GetView(parentComponent));
            Assert.IsNull(_runner.Registry.GetView(childComponent));
        }

        [UnityTest]
        public IEnumerator UnityFrameLoop_ShouldDriveAwakeUpdateAndDestroy()
        {
            var scene = Track(new RegressionScene("LifecycleScene"));
            var entity = scene.AddChild<LifecycleProbeEntity>();

            Assert.AreEqual(1, entity.AwakeCount);
            Assert.AreEqual(0, entity.UpdateCount);
            Assert.AreEqual(0, entity.DestroyCount);

            yield return null;

            Assert.Greater(entity.UpdateCount, 0);

            int updateCount = entity.UpdateCount;

            entity.Destroy();

            yield return null;

            Assert.IsTrue(entity.IsDestroyed);
            Assert.AreEqual(1, entity.DestroyCount);

            yield return null;

            Assert.AreEqual(updateCount, entity.UpdateCount);
        }

        [UnityTest]
        public IEnumerator DependencyComponent_ShouldWaitForRequiredComponent()
        {
            var scene = Track(new RegressionScene("DependencyScene"));
            var host = scene.AddChild<DependencyHostEntity>();
            var dependent = host.AddComponent<DependentProbeComponent>();

            yield return null;

            Assert.IsFalse(dependent.AreAllDependenciesMet);
            Assert.AreEqual(0, dependent.UpdateCount);

            host.AddComponent<RequiredProbeComponent>();

            yield return null;

            Assert.IsTrue(dependent.AreAllDependenciesMet);
            Assert.That(dependent.ActivationChanges, Is.EqualTo(new[] { true }));
            Assert.Greater(dependent.UpdateCount, 0);

            int updateCount = dependent.UpdateCount;

            host.RemoveComponent<RequiredProbeComponent>();

            yield return null;

            Assert.IsFalse(dependent.AreAllDependenciesMet);
            Assert.That(dependent.ActivationChanges, Is.EqualTo(new[] { true, false }));
            Assert.AreEqual(updateCount, dependent.UpdateCount);
        }

        [UnityTest]
        public IEnumerator LifecycleGate_ShouldKeepEntityInTreeButBlockRuntimeUntilReady()
        {
            var scene = Track(new RegressionScene("GateScene"));
            var entity = scene.AddChild<GatedRuntimeEntity>();

            yield return null;

            Assert.AreSame(scene, entity.Parent);
            Assert.IsTrue(scene.ContainsChild(entity.Id));
            Assert.NotNull(_runner.Registry.GetView(entity));
            Assert.IsFalse(entity.IsReady);
            Assert.IsFalse(entity.CanRun);
            Assert.AreEqual(0, entity.UpdateCount);

            entity.SetCanRun(true);

            yield return null;

            Assert.AreEqual(0, entity.UpdateCount);

            entity.SetReady(true);

            yield return null;

            Assert.IsTrue(entity.IsReady);
            Assert.IsTrue(entity.CanRun);
            Assert.Greater(entity.UpdateCount, 0);
        }

        [UnityTest]
        public IEnumerator LifecycleGateDependency_ShouldTreatUnreadyRequiredComponentAsMissing()
        {
            var scene = Track(new RegressionScene("GateDependencyScene"));
            var host = scene.AddChild<DependencyHostEntity>();
            var required = host.AddComponent<GatedRequiredComponent>();
            var dependent = host.AddComponent<GatedDependentProbeComponent>();

            yield return null;

            Assert.IsFalse(required.IsReady);
            Assert.IsFalse(dependent.AreAllDependenciesMet);
            Assert.AreEqual(0, dependent.UpdateCount);

            required.SetReady(true);

            yield return null;

            Assert.IsTrue(dependent.AreAllDependenciesMet);
            Assert.That(dependent.ActivationChanges, Is.EqualTo(new[] { true }));
            Assert.Greater(dependent.UpdateCount, 0);
        }

        private T Track<T>(T scene) where T : Scene
        {
            var registeredScene = (T)World.Instance.AddScene(scene.Name, scene);
            _scenes.Add(registeredScene);
            return registeredScene;
        }

        private sealed class RegressionScene : Scene
        {
            public RegressionScene(string name) : base(name)
            {
            }
        }

        private sealed class TreeProbeEntity : Entity, IAwake, IDestroy
        {
            public int DestroyCount { get; private set; }

            public void Awake()
            {
            }

            public void OnDestroy()
            {
                DestroyCount++;
            }
        }

        private sealed class TreeProbeComponent : Entity, IAwake, IDestroy
        {
            public int DestroyCount { get; private set; }

            public void Awake()
            {
            }

            public void OnDestroy()
            {
                DestroyCount++;
            }
        }

        private sealed class LifecycleProbeEntity : Entity, IAwake, IUpdate, IDestroy
        {
            public int AwakeCount { get; private set; }

            public int UpdateCount { get; private set; }

            public int DestroyCount { get; private set; }

            public void Awake()
            {
                AwakeCount++;
            }

            public void Update(float time)
            {
                UpdateCount++;
            }

            public void OnDestroy()
            {
                DestroyCount++;
            }
        }

        private sealed class DependencyHostEntity : Entity, IAwake
        {
            public void Awake()
            {
            }
        }

        private sealed class RequiredProbeComponent : Entity, IAwake
        {
            public void Awake()
            {
            }
        }

        [DependsOn(typeof(RequiredProbeComponent))]
        private sealed class DependentProbeComponent : DependentComponentBase, IAwake, IUpdate
        {
            public int UpdateCount { get; private set; }

            public List<bool> ActivationChanges { get; } = new List<bool>();

            public void Awake()
            {
            }

            public void Update(float time)
            {
                UpdateCount++;
            }

            protected override void OnActivationChanged(bool isActive)
            {
                ActivationChanges.Add(isActive);
            }
        }

        private sealed class GatedRuntimeEntity : Entity, IAwake, IUpdate, IEntityLifecycleGate
        {
            private bool _canRun;

            public bool IsReady { get; private set; }

            public bool CanRun => IsReady && _canRun;

            public int UpdateCount { get; private set; }

            public void Awake()
            {
            }

            public void Update(float time)
            {
                UpdateCount++;
            }

            public void SetReady(bool isReady)
            {
                IsReady = isReady;
            }

            public void SetCanRun(bool canRun)
            {
                _canRun = canRun;
            }
        }

        private sealed class GatedRequiredComponent : Entity, IAwake, IEntityLifecycleGate
        {
            public bool IsReady { get; private set; }

            public bool CanRun => IsReady;

            public void Awake()
            {
            }

            public void SetReady(bool isReady)
            {
                IsReady = isReady;
            }
        }

        [DependsOn(typeof(GatedRequiredComponent))]
        private sealed class GatedDependentProbeComponent : DependentComponentBase, IAwake, IUpdate
        {
            public int UpdateCount { get; private set; }

            public List<bool> ActivationChanges { get; } = new List<bool>();

            public void Awake()
            {
            }

            public void Update(float time)
            {
                UpdateCount++;
            }

            protected override void OnActivationChanged(bool isActive)
            {
                ActivationChanges.Add(isActive);
            }
        }
    }
}
