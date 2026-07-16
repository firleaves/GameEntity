using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class UISystemPlayModeTests
    {
        private GameObject _frameworkRoot;
        private TestScene _scene;
        private UISystemEntity _uiSystem;
        private FakeInstancePool _instancePool;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            TestPanel.Reset();
            _frameworkRoot = new GameObject("FrameworkUITestRoot");
            _instancePool = new FakeInstancePool();
            _scene = (TestScene)World.Instance.AddScene(
                "FrameworkUITestScene",
                new TestScene("FrameworkUITestScene"));
            _uiSystem = _scene.AddChild<UISystemEntity, UISystemDependencies>(new UISystemDependencies
            {
                Options = UIOptions.CreateDefault(),
                InstancePool = _instancePool,
                FrameworkRoot = _frameworkRoot.transform,
                AutoCreateEventSystem = false
            });
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene != null && !_scene.IsDestroyed)
            {
                _scene.Destroy();
            }

            World.Instance.RemoveScene("FrameworkUITestScene");
            World.Instance.Dispose();
            _scene = null;
            _uiSystem = null;
            _instancePool = null;

            if (_frameworkRoot != null)
            {
                Object.Destroy(_frameworkRoot);
            }

            _frameworkRoot = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenAsync_CreatesUIEntityAndView()
        {
            TestPanel panel = null;
            yield return _uiSystem.OpenAsync<TestPanel>(new UIOpenParams
            {
                ViewKey = AssetKey.Main<GameObject>("Tests/UI/TestPanel"),
                Group = "Main",
                Depth = 12
            }).ToCoroutine(result => panel = result);

            Assert.NotNull(panel);
            Assert.NotNull(panel.View);
            Assert.NotNull(panel.View.GameObject);
            Assert.AreEqual("Main", panel.Group);
            Assert.AreEqual(12, panel.Depth);
            Assert.AreEqual(1, TestPanel.OpenCount);
            Assert.AreEqual(1, _instancePool.RentCount);

            var snapshot = _uiSystem.GetSnapshot();
            Assert.AreEqual(1, snapshot.OpenUIs.Count);
            Assert.AreEqual(nameof(TestPanel), snapshot.OpenUIs[0].UIName);
            Assert.AreEqual("Tests/UI/TestPanel", snapshot.OpenUIs[0].ViewLocation);
        }

        [UnityTest]
        public IEnumerator CloseAsync_ReleasesViewToInstancePool()
        {
            TestPanel panel = null;
            yield return _uiSystem.OpenAsync<TestPanel>(new UIOpenParams
            {
                ViewKey = AssetKey.Main<GameObject>("Tests/UI/TestPanel")
            }).ToCoroutine(result => panel = result);

            yield return _uiSystem.CloseAsync(panel).ToCoroutine();
            yield return null;

            Assert.AreEqual(1, TestPanel.CloseCount);
            Assert.AreEqual(1, _instancePool.ReturnCount);
            Assert.AreEqual(0, _uiSystem.GetSnapshot().OpenUIs.Count);
        }

        [UnityTest]
        public IEnumerator SingleReuse_ReturnsExistingUI()
        {
            TestPanel.RefocusCount = 0;
            TestPanel first = null;
            TestPanel second = null;

            yield return _uiSystem.OpenAsync<TestPanel>(new UIOpenParams
            {
                ViewKey = AssetKey.Main<GameObject>("Tests/UI/TestPanel"),
                ReusePolicy = UIReusePolicy.Single
            }).ToCoroutine(result => first = result);

            yield return _uiSystem.OpenAsync<TestPanel>(new UIOpenParams
            {
                ViewKey = AssetKey.Main<GameObject>("Tests/UI/TestPanel"),
                ReusePolicy = UIReusePolicy.Single
            }).ToCoroutine(result => second = result);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, _instancePool.RentCount);
            Assert.AreEqual(1, TestPanel.RefocusCount);
        }

        [UnityTest]
        public IEnumerator OwnedEventSystem_IsDestroyedWithUISystem()
        {
            Assert.IsNull(EventSystem.current, "测试开始前不应存在外部 EventSystem。");
            var ownedSystem = _scene.AddChild<UISystemEntity, UISystemDependencies>(new UISystemDependencies
            {
                Options = UIOptions.CreateDefault(),
                InstancePool = _instancePool,
                FrameworkRoot = _frameworkRoot.transform,
                AutoCreateEventSystem = true
            });
            yield return null;

            var eventSystemObject = EventSystem.current != null
                ? EventSystem.current.gameObject
                : null;
            Assert.NotNull(eventSystemObject);

            ownedSystem.Destroy();
            yield return null;

            Assert.IsTrue(eventSystemObject == null);
            Assert.IsNull(EventSystem.current);
        }

        private sealed class TestScene : Scene
        {
            public TestScene(string name) : base(name)
            {
            }
        }

        public sealed class TestPanel : UIEntity
        {
            public static int OpenCount;
            public static int CloseCount;
            public static int RefocusCount;

            public static void Reset()
            {
                OpenCount = 0;
                CloseCount = 0;
                RefocusCount = 0;
            }

            protected override UniTask OnOpenAsync(UIOpenContext context)
            {
                OpenCount++;
                return UniTask.CompletedTask;
            }

            protected override UniTask OnCloseAsync(UICloseContext context)
            {
                CloseCount++;
                return UniTask.CompletedTask;
            }

            protected override void OnRefocus()
            {
                RefocusCount++;
            }
        }

        private sealed class FakeInstancePool : IInstancePool
        {
            public int RentCount;
            public int ReturnCount;

            public UniTask<InstanceRef> RentAsync(
                AssetKey prefabKey,
                Transform parent = null,
                InstanceRentOptions options = null,
                CancellationToken ct = default)
            {
                RentCount++;
                var go = new GameObject(prefabKey.Location);
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                }

                go.SetActive(options == null || options.SetActive);
                return UniTask.FromResult(new InstanceRef(prefabKey, go, this));
            }

            public UniTask WarmupAsync(
                AssetKey prefabKey,
                int count,
                Transform inactiveRoot = null,
                PoolPolicy policy = null,
                CancellationToken ct = default)
            {
                return UniTask.CompletedTask;
            }

            public void Return(InstanceRef instanceRef)
            {
                if (instanceRef?.GameObject != null)
                {
                    Return(instanceRef.GameObject);
                }
            }

            public bool Return(GameObject instance)
            {
                if (instance == null)
                {
                    return false;
                }

                ReturnCount++;
                Object.Destroy(instance);
                return true;
            }

            public int ReleaseUnused(AssetReleaseReason reason = AssetReleaseReason.Manual)
            {
                return 0;
            }

            public void ReleasePool(AssetKey prefabKey, bool force = false)
            {
            }

            public InstancePoolSnapshot GetSnapshot()
            {
                return new InstancePoolSnapshot();
            }
        }
    }
}
