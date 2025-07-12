using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GE;
using Object = UnityEngine.Object;

namespace GE.Runtime.Tests
{
    public class EntityTests
    {
        private GameObject _gameObject;
        private GameEntityIniter _initer;
        private TestScene _scene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // 创建测试场景
            _gameObject = new GameObject("GameEntityTest");
            _initer = _gameObject.AddComponent<GameEntityIniter>();

            // 等待初始化完成
            yield return null;

            // 创建测试场景
            _scene = new TestScene("TestScene");
            World.Instance.AddScene("TestScene", _scene);
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
        public IEnumerator Entity_WhenCreated_HasUniqueId()
        {
            // Arrange
            var entity1 = _scene.AddChild<TestEntity>();
            var entity2 = _scene.AddChild<TestEntity>();
            yield return null;

            try
            {
                Assert.AreNotEqual(entity1.Id, entity2.Id, "Entities should have unique IDs");
                Assert.AreNotEqual(entity1.InstanceId, entity2.InstanceId, "Entities should have unique InstanceIds");
            }
            finally
            {
                _scene.RemoveChild(entity1.Id);
                _scene.RemoveChild(entity2.Id);
            }
            // Assert
        }

        [UnityTest]
        public IEnumerator Entity_WhenAddedAsChild_IsInParentChildrenList()
        {
            // Arrange
            var parent = _scene.AddChild<TestEntity>();
            yield return null;

            // Act
            var child = parent.AddChild<TestEntity>();
            yield return null;

            try
            {
                // Assert
                Assert.IsTrue(parent.Children.ContainsKey(child.Id), "Parent's children list should contain child");
                Assert.AreEqual(parent, child.Parent, "Child's parent should be set correctly");
            }
            finally
            {
                _scene.RemoveChild(parent.Id);
            }
        }

        [UnityTest]
        public IEnumerator Entity_WhenAddedAsComponent_IsInComponentsList()
        {
            // Arrange
            var entity = _scene.AddChild<TestEntity>();
            yield return null;

            // Act
            var component = entity.AddComponent<TestComponent>();
            yield return null;

            try
            {
                // Assert
                var testComponent = entity.GetComponent<TestComponent>();
                Assert.IsTrue(testComponent.Id == component.Id, "Entity's components list should contain the component");
                Assert.IsTrue(testComponent.IsComponent, "Component flag should be set");
            }
            finally
            {
                _scene.RemoveChild(entity.Id);
            }
        }

        [UnityTest]
        public IEnumerator Entity_WhenRemoved_IsRemovedFromTree()
        {
            // Arrange
            var parent = _scene.AddChild<TestEntity>();
            var child = parent.AddChild<TestEntity>();
            yield return null;

            // Act
            child.Dispose();
            yield return null;

            try
            {
                // Assert
                Assert.IsFalse(parent.Children.ContainsKey(child.Id), "Parent should not contain disposed child");
                Assert.IsTrue(child.IsDisposed, "Child should be marked as disposed");
            }
            finally
            {
                _scene.RemoveChild(parent.Id);
            }
        }

        [UnityTest]
        public IEnumerator Entity_WhenMoved_UpdatesTreeStructure()
        {
            // Arrange
            var parent1 = _scene.AddChild<TestEntity>();
            var parent2 = _scene.AddChild<TestEntity>();
            var child = parent1.AddChild<TestEntity>();
            yield return null;

            // Act
            child.Parent = parent2;
            yield return null;

            try
            {
                // Assert
                Assert.IsFalse(parent1.Children.ContainsKey(child.Id), "Original parent should not contain child");
                Assert.IsTrue(parent2.Children.ContainsKey(child.Id), "New parent should contain child");
                Assert.AreEqual(parent2, child.Parent, "Child's parent should be updated");

            }
            finally
            {
                _scene.RemoveChild(parent1.Id);
                _scene.RemoveChild(parent2.Id);
            }
        }

        private class TestEntity : Entity, IAwake
        {
            // 测试用实体
            public void Awake()
            {
            }
        }

        private class TestComponent : Entity, IAwake
        {
            // 测试用组件
            public void Awake()
            {
            }
        }
    }
}