using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GE;

namespace GE.Runtime.Tests
{
    public class SceneTests
    {
        private GameObject _gameObject;
        private List<Entity> _entities;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _gameObject = new GameObject("GameEntityTest");
            _gameObject.AddComponent<GameEntityIniter>();
            _entities = new List<Entity>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_gameObject != null)
            {
                Object.Destroy(_gameObject);
            }

            _entities.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_WhenDisposed_DisposesEntireEntityTree()
        {
            try
            {
                // Arrange - 创建一个复杂的实体树
                var scene = World.Instance.AddScene("TestScene", new TestScene("TestScene"));
                yield return null;

                // 创建多层实体树
                var entity1 = scene.AddChild<TestEntity>();
                _entities.Add(entity1);

                var child1 = entity1.AddChild<TestEntity>();
                _entities.Add(child1);
                _entities.Add(entity1.AddChild<TestEntity>());
                _entities.Add(entity1.AddChild<TestEntity>());
                _entities.Add(child1.AddChild<TestEntity>());

                _entities.Add(entity1.AddComponent<TestComponent>());
                _entities.Add(child1.AddComponent<TestComponent>());

                yield return null;

                // Act - 销毁场景
                scene.Dispose();
                yield return null;

                // Assert

                foreach (var entity in _entities)
                {
                    if (entity is TestEntity testEntity)
                    {
                        Assert.IsTrue(testEntity.WasDestroyCalled,
                            $"Entity {entity.Id} should have Destroy called");
                    }
                    else if (entity is TestComponent testComponent)
                    {
                        Assert.IsTrue(testComponent.WasDestroyCalled,
                            $"Component {entity.Id} should have Destroy called");
                    }

                    Assert.IsTrue(entity.IsDisposed,
                        $"Entity {entity.Id} should be disposed");
                }
            }
            finally
            {
                World.Instance.RemoveScene("TestScene");
            }
        }

        [UnityTest]
        public IEnumerator Scene_WhenComponentRemoved_DisposesComponentOnly()
        {
            try
            {
                // Arrange
                var scene = World.Instance.AddScene("TestScene", new TestScene("TestScene"));
                yield return null;

                var entity = scene.AddChild<TestEntity>();
                var component = entity.AddComponent<TestComponent>();
                yield return null;

                // Act
                entity.RemoveComponent<TestComponent>();
                yield return null;

                var destroyedComponent = entity.GetComponent<TestComponent>();

                // Assert

                Assert.IsFalse(destroyedComponent?.Id == component.Id, "Entity should not contain removed component");
                Assert.IsTrue(component.WasDestroyCalled,
                    "Component's Destroy should be called");
                Assert.IsTrue(component.IsDisposed,
                    "Component should be disposed");
                Assert.IsFalse(entity.IsDisposed,
                    "Entity should not be disposed");
                Assert.IsTrue(scene.Children.ContainsKey(entity.Id),
                    "Scene should still contain the entity");
            }
            finally
            {
                World.Instance.RemoveScene("TestScene");
            }
        }

        [UnityTest]
        public IEnumerator Scene_WhenEntityMoved_ChangeSceneReferences()
        {
            try
            {
                // Arrange
                var scene1 = World.Instance.AddScene("TestScene1", new TestScene("TestScene1"));
                var scene2 = World.Instance.AddScene("TestScene2", new TestScene("TestScene2"));
                yield return null;

                var entity = scene1.AddChild<TestEntity>();
                var child = entity.AddChild<TestEntity>();
                yield return null;

                // Act
                entity.Parent = scene2;
                yield return null;

                // Assert

                Assert.IsFalse(scene1.Children.ContainsKey(entity.Id), "Original scene should not contain moved entity");
                Assert.IsTrue(scene2.Children.ContainsKey(entity.Id), "New scene should contain moved entity");
                Assert.IsTrue(entity.Children.ContainsKey(child.Id), "Entity should maintain its children");
                Assert.AreEqual(scene2, entity.Parent, "Entity's parent should be updated");
            }
            finally
            {
                World.Instance.RemoveScene("TestScene1");
                World.Instance.RemoveScene("TestScene2");
            }
        }

        private class TestEntity : Entity, IAwake, IDestroy
        {
            public bool WasDestroyCalled { get; private set; }

            public void Awake()
            {
            }

            public void OnDestroy()
            {
                WasDestroyCalled = true;
            }
        }

        private class TestComponent : Entity, IAwake, IDestroy
        {
            public bool WasDestroyCalled { get; private set; }

            public void Awake()
            {
            }

            public void OnDestroy()
            {
                WasDestroyCalled = true;
            }
        }
    }
}