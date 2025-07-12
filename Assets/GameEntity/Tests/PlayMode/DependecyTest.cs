using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace GE.Runtime.Tests
{
    public class DependecyTest
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
                GameObject.Destroy(_gameObject);
            }

            yield return null;
        }

        private class TestEntity : Entity, IAwake
        {
            public void Awake()
            {
            }
        }

        private class AComponent : Entity, IAwake
        {
            public int A = 0;

            public void Awake()
            {
                A = 1;
            }
        }

        private class CComponent : Entity, IAwake
        {
            public int C = 0;

            public void Awake()
            {
                C = 1;
            }
        }


        [DependsOn(typeof(AComponent))]
        private class BComponent : DependentComponentBase, IAwake, IUpdate
        {

            public bool WasUpdateCalled = false;
            public void Awake()
            {
            }

            protected override void OnActivationChanged(bool isActive)
            {

            }

            public void Update(float time)
            {
                WasUpdateCalled = true;
            }
        }

        private class DComponent : DependentComponentBase, IAwake
        {


            public void Awake()
            {
            }

            public override Type[] GetDependencyTypes()
            {
                return new Type[] { typeof(AComponent), typeof(CComponent) };
            }

        }

        [UnityTest]
        public System.Collections.IEnumerator DependencyComponent_WhenLoaded_CompletesSuccessfully() =>
                  UniTask.ToCoroutine(async () =>
                  {
                      var entity = _testScene.AddChild<TestEntity>();
                      var bEntity = entity.AddComponent<BComponent>();
                      var dEntity = entity.AddComponent<DComponent>();

                      Assert.IsFalse(bEntity.WasUpdateCalled);
                      Assert.IsFalse(dEntity.AreAllDependenciesMet);


                      var cEntity = entity.AddComponent<CComponent>();
                      Assert.IsFalse(dEntity.AreAllDependenciesMet);

                      var aEntity = entity.AddComponent<AComponent>();


                      Assert.IsTrue(dEntity.AreAllDependenciesMet);



                      await UniTask.DelayFrame(2);
                      try
                      {
                          Assert.IsTrue(bEntity.WasUpdateCalled);
                          Assert.IsTrue(dEntity.AreAllDependenciesMet);
                      }
                      finally
                      {
                          _testScene.RemoveChild(entity.Id);
                      }
                  });



    }
}
