using System;
using GameEntity;
using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class FrameworkSceneTests
    {
        private FrameworkScene _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = (FrameworkScene)World.Instance.AddScene(
                "FrameworkSceneTests",
                new FrameworkScene("FrameworkSceneTests"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene != null && !_scene.IsDestroyed)
            {
                _scene.Destroy();
            }

            World.Instance.Dispose();
            _scene = null;
        }

        [Test]
        public void SetService_RejectsDifferentServiceWithSameContract()
        {
            _scene.SetService<ITestService>(new DisposableTestService());

            Assert.Throws<FrameworkException>(() =>
                _scene.SetService<ITestService>(new DisposableTestService()));
        }

        [Test]
        public void Destroy_DisposesNonEntityServiceOnceAcrossMultipleContracts()
        {
            var service = new DisposableTestService();
            _scene.SetService<ITestService>(service);
            _scene.SetService<IDisposable>(service);

            _scene.Destroy();

            Assert.AreEqual(1, service.DisposeCount);
        }

        private interface ITestService
        {
        }

        private sealed class DisposableTestService : ITestService, IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
