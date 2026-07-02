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
        private GameEntityUnityBootstrap _bootstrap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = new GameObject("GameEntity.Unity.RegressionRoot");
            _bootstrap = _root.AddComponent<GameEntityUnityBootstrap>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (Scene scene in _scenes)
            {
                if (scene != null && !scene.IsDisposed)
                {
                    scene.Dispose();
                }
            }

            _scenes.Clear();

            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            _bootstrap = null;

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
            Assert.IsTrue(parent1.Children.ContainsKey(child.Id));
            Assert.AreSame(parent1, child.Parent);
            Assert.AreSame(component, child.GetComponent<TreeProbeComponent>());
            Assert.AreSame(child, component.Parent);
            Assert.AreEqual(child.Id, component.Id);

            ComponentView parent1View = _bootstrap.Registry.GetView(parent1);
            ComponentView parent2View = _bootstrap.Registry.GetView(parent2);
            ComponentView childView = _bootstrap.Registry.GetView(child);
            ComponentView componentView = _bootstrap.Registry.GetView(component);

            Assert.NotNull(parent1View);
            Assert.NotNull(parent2View);
            Assert.NotNull(childView);
            Assert.NotNull(componentView);
            Assert.AreSame(parent1View.transform, childView.transform.parent);
            Assert.AreSame(childView.transform, componentView.transform.parent);

            child.Parent = parent2;

            yield return null;

            Assert.IsFalse(parent1.Children.ContainsKey(child.Id));
            Assert.IsTrue(parent2.Children.ContainsKey(child.Id));
            Assert.AreSame(parent2, child.Parent);
            Assert.AreSame(parent2View.transform, childView.transform.parent);

            child.Dispose();

            yield return null;

            Assert.IsFalse(parent2.Children.ContainsKey(child.Id));
            Assert.IsTrue(child.IsDisposed);
            Assert.IsTrue(component.IsDisposed);
            Assert.IsNull(_bootstrap.Registry.GetView(child));
            Assert.IsNull(_bootstrap.Registry.GetView(component));
        }

        [UnityTest]
        public IEnumerator RemoveComponent_ShouldDisposeComponentOnlyAndKeepEntityView()
        {
            var scene = Track(new RegressionScene("ComponentScene"));
            var entity = scene.AddChild<TreeProbeEntity>();
            var component = entity.AddComponent<TreeProbeComponent>();

            yield return null;

            ComponentView entityView = _bootstrap.Registry.GetView(entity);
            ComponentView componentView = _bootstrap.Registry.GetView(component);

            Assert.NotNull(entityView);
            Assert.NotNull(componentView);
            Assert.AreSame(entityView.transform, componentView.transform.parent);

            entity.RemoveComponent<TreeProbeComponent>();

            yield return null;

            Assert.IsNull(entity.GetComponent<TreeProbeComponent>());
            Assert.AreEqual(0, entity.ComponentsCount());
            Assert.IsFalse(entity.IsDisposed);
            Assert.IsTrue(scene.Children.ContainsKey(entity.Id));
            Assert.IsTrue(component.IsDisposed);
            Assert.AreEqual(1, component.DestroyCount);
            Assert.NotNull(_bootstrap.Registry.GetView(entity));
            Assert.IsNull(_bootstrap.Registry.GetView(component));
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

            Assert.NotNull(_bootstrap.Registry.GetView(scene));
            Assert.NotNull(_bootstrap.Registry.GetView(parent));
            Assert.NotNull(_bootstrap.Registry.GetView(child));
            Assert.NotNull(_bootstrap.Registry.GetView(grandchild));
            Assert.NotNull(_bootstrap.Registry.GetView(parentComponent));
            Assert.NotNull(_bootstrap.Registry.GetView(childComponent));

            scene.Dispose();

            yield return null;

            Assert.IsTrue(scene.IsDisposed);
            Assert.IsTrue(parent.IsDisposed);
            Assert.IsTrue(child.IsDisposed);
            Assert.IsTrue(grandchild.IsDisposed);
            Assert.IsTrue(parentComponent.IsDisposed);
            Assert.IsTrue(childComponent.IsDisposed);
            Assert.AreEqual(1, parent.DestroyCount);
            Assert.AreEqual(1, child.DestroyCount);
            Assert.AreEqual(1, grandchild.DestroyCount);
            Assert.AreEqual(1, parentComponent.DestroyCount);
            Assert.AreEqual(1, childComponent.DestroyCount);
            Assert.IsNull(_bootstrap.Registry.GetView(scene));
            Assert.IsNull(_bootstrap.Registry.GetView(parent));
            Assert.IsNull(_bootstrap.Registry.GetView(child));
            Assert.IsNull(_bootstrap.Registry.GetView(grandchild));
            Assert.IsNull(_bootstrap.Registry.GetView(parentComponent));
            Assert.IsNull(_bootstrap.Registry.GetView(childComponent));
        }

        [UnityTest]
        public IEnumerator UnityFrameLoop_ShouldDriveAwakeUpdateLateUpdateAndDestroy()
        {
            var scene = Track(new RegressionScene("LifecycleScene"));
            var entity = scene.AddChild<LifecycleProbeEntity>();

            Assert.AreEqual(1, entity.AwakeCount);
            Assert.AreEqual(0, entity.UpdateCount);
            Assert.AreEqual(0, entity.LateUpdateCount);
            Assert.AreEqual(0, entity.DestroyCount);

            yield return null;

            Assert.Greater(entity.UpdateCount, 0);
            Assert.Greater(entity.LateUpdateCount, 0);

            int updateCount = entity.UpdateCount;
            int lateUpdateCount = entity.LateUpdateCount;

            entity.Dispose();

            yield return null;

            Assert.IsTrue(entity.IsDisposed);
            Assert.AreEqual(1, entity.DestroyCount);

            yield return null;

            Assert.AreEqual(updateCount, entity.UpdateCount);
            Assert.AreEqual(lateUpdateCount, entity.LateUpdateCount);
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
            Assert.IsTrue(scene.Children.ContainsKey(entity.Id));
            Assert.NotNull(_bootstrap.Registry.GetView(entity));
            Assert.IsFalse(entity.IsReady);
            Assert.IsFalse(entity.CanRun);
            Assert.AreEqual(0, entity.UpdateCount);
            Assert.AreEqual(0, entity.LateUpdateCount);

            entity.SetCanRun(true);

            yield return null;

            Assert.AreEqual(0, entity.UpdateCount);
            Assert.AreEqual(0, entity.LateUpdateCount);

            entity.SetReady(true);

            yield return null;

            Assert.IsTrue(entity.IsReady);
            Assert.IsTrue(entity.CanRun);
            Assert.Greater(entity.UpdateCount, 0);
            Assert.Greater(entity.LateUpdateCount, 0);
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
            _scenes.Add(scene);
            return scene;
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

        private sealed class LifecycleProbeEntity : Entity, IAwake, IUpdate, ILateUpdate, IDestroy
        {
            public int AwakeCount { get; private set; }

            public int UpdateCount { get; private set; }

            public int LateUpdateCount { get; private set; }

            public int DestroyCount { get; private set; }

            public void Awake()
            {
                AwakeCount++;
            }

            public void Update(float time)
            {
                UpdateCount++;
            }

            public void LateUpdate()
            {
                LateUpdateCount++;
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

        private sealed class GatedRuntimeEntity : Entity, IAwake, IUpdate, ILateUpdate, IEntityLifecycleGate
        {
            private bool _canRun;

            public bool IsReady { get; private set; }

            public bool CanRun => IsReady && _canRun;

            public int UpdateCount { get; private set; }

            public int LateUpdateCount { get; private set; }

            public void Awake()
            {
            }

            public void Update(float time)
            {
                UpdateCount++;
            }

            public void LateUpdate()
            {
                LateUpdateCount++;
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
