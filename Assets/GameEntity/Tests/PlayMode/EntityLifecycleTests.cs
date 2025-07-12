using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GE;

namespace GE.Runtime.Tests
{
    [TestFixture]
    public class EntityLifecycleTests
    {
        private GameObject _gameObject;

        private TestScene _testScene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // 创建测试场景
            _gameObject = new GameObject("GameEntityTest");
            _gameObject.AddComponent<GameEntityIniter>();
            // yield return null;
            _testScene = new TestScene("TestScene");
            World.Instance.AddScene("TestScene", _testScene);
            // 等待一帧确保初始化完成
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // 清理测试场景
            if (_gameObject != null)
            {
                World.Instance.RemoveScene("TestScene");
                Object.Destroy(_gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EntitySystem_WhenUpdate_ComponentsAreUpdated()
        {
            var testEntity = _testScene.AddChild<TestEntityWithUpdate>();

            yield return null;

            Assert.IsTrue(testEntity.WasUpdateCalled);
        }

        [UnityTest]
        public IEnumerator EntitySystem_WhenAwakeAndDestroy_MethodsAreCalled()
        {
            var testEntity = _testScene.AddChild<TestEntityWithAwakeAndDestroy>();

            yield return null;

            Assert.IsTrue(testEntity.WasAwakeCalled);

            _testScene.RemoveChild(testEntity.Id);

            yield return null;

            Assert.IsTrue(testEntity.WasDestroyCalled);
        }

        private class TestEntityWithAwakeAndDestroy : Entity, IAwake, IDestroy
        {
            public bool WasAwakeCalled { get; private set; }
            public bool WasDestroyCalled { get; private set; }

            public void Awake()
            {
                WasAwakeCalled = true;
            }

            public void OnDestroy()
            {
                WasDestroyCalled = true;
            }
        }

        [UnityTest]
        public IEnumerator EntitySystem_WhenLateUpdate_ComponentsAreLateUpdated()
        {
            var testEntity = _testScene.AddChild<TestEntityWithLateUpdate>();

            yield return null;

            Assert.IsTrue(testEntity.WasLateUpdateCalled);
        }

        private class TestEntityWithUpdate : Entity, IAwake, IUpdate
        {
            public bool WasUpdateCalled { get; private set; }

            public void Awake()
            {
            }

   

            public void Update(float time)
            {
                 WasUpdateCalled = true;
            }
        }

        private class TestEntityWithLateUpdate : Entity, IAwake, ILateUpdate
        {
            public bool WasLateUpdateCalled { get; private set; }

            public void Awake()
            {
            }

            public void LateUpdate()
            {
                WasLateUpdateCalled = true;
            }
        }
    }
}