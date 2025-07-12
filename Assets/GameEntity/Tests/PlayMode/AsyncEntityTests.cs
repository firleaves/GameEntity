using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using GE;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GE.Runtime.Tests
{
    public class AsyncEntityTests
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


        private class ParentEntity : Entity, IAwake
        {
            public bool WasAwakeCalled { get; private set; }

            public void Awake()
            {
                WasAwakeCalled = true;
            }
        }

        private class TestAsyncEntity : AsyncEntity, IAwake
        {
            public bool WasLoaded { get; private set; }
            public bool WasOnLoadedCalled { get; private set; }
            public bool WasCancelled { get; private set; }
            public int LoadDelayMs { get; set; } = 1000;

            protected override async UniTask OnLoadAsync(CancellationToken cancelToken)
            {
                try
                {
                    await UniTask.Delay(LoadDelayMs, cancellationToken: cancelToken);
                    WasLoaded = true;
                }
                catch (OperationCanceledException)
                {
                    WasCancelled = true;
                    throw;
                }
            }

            protected override void OnLoaded()
            {
                WasOnLoadedCalled = true;
            }

            public void Awake()
            {
            }
        }

        private class TestEntity : Entity, IAwake
        {
            public bool WasAwakeCalled { get; private set; }

            public void Awake()
            {
                WasAwakeCalled = true;
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator AsyncEntityComponent_WhenLoaded_CompletesSuccessfully() =>
            UniTask.ToCoroutine(async () =>
            {
                var entity = _testScene.AddComponent<ParentEntity>();
                var asyncEntity = await entity.AddComponentAsync<TestAsyncEntity>();
                var testEntity = entity.AddComponent<TestEntity>();
                try
                {
                    Assert.IsTrue(asyncEntity.IsLoaded);
                    Assert.IsTrue(asyncEntity.WasLoaded);
                    Assert.IsTrue(asyncEntity.WasOnLoadedCalled);
                    Assert.IsTrue(testEntity.WasAwakeCalled);
                }
                finally
                {
                    _testScene.RemoveComponent<ParentEntity>();
                }
            });


        [UnityTest]
        public System.Collections.IEnumerator AsyncEntityChild_WhenLoaded_CompletesSuccessfully() =>
            UniTask.ToCoroutine(async () =>
            {
                var entity = _testScene.AddChild<ParentEntity>();
                var asyncEntity = await entity.AddChildAsync<TestAsyncEntity>();
                var testEntity = entity.AddChild<TestEntity>();
                try
                {
                    Assert.IsTrue(asyncEntity.IsLoaded);
                    Assert.IsTrue(asyncEntity.WasLoaded);
                    Assert.IsTrue(asyncEntity.WasOnLoadedCalled);
                    Assert.IsTrue(testEntity.WasAwakeCalled);
                }
                finally
                {
                    _testScene.RemoveComponent<ParentEntity>();
                }
            });


       [UnityTest]
        public System.Collections.IEnumerator AsyncEntity_WhenCancelled() =>
            UniTask.ToCoroutine(async () =>
            {
                var entity = _testScene.AddChild<TestAsyncEntity>();
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var asyncEntity = await entity.AddComponentAsync<TestAsyncEntity>(cts.Token);
                if (asyncEntity == null)
                {
                    Assert.True(true);
                    return;
                }
                var testEntity = asyncEntity.AddChild<TestEntity>();
                try
                {
                    Assert.IsNull(asyncEntity);
                    Assert.IsNull(testEntity);
                }
                finally
                {
                    _testScene.RemoveComponent<ParentEntity>();
                }

            });


        [UnityTest]
        public System.Collections.IEnumerator AsyncEntity_WhenLoadCancelled() =>
            UniTask.ToCoroutine(async () =>
            {
                var entity = _testScene.AddChild<TestAsyncEntity>();
                var cts = new CancellationTokenSource();
                 _ = UniTask.Delay(100).ContinueWith(() => cts.Cancel());
                var asyncEntity = await entity.AddComponentAsync<TestAsyncEntity>(cts.Token);
                if (asyncEntity == null)
                {
                    Assert.True(true);
                }
                var testEntity = entity.AddChild<TestEntity>();
                try
                {
                    Assert.IsNull(asyncEntity);
                    Assert.IsTrue(testEntity.WasAwakeCalled);
                }
                finally
                {
                    _testScene.RemoveComponent<ParentEntity>();
                }
            });
    }
}