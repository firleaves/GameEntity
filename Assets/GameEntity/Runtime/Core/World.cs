using System;
using System.Collections.Generic;
using UnityEngine;

namespace GE
{
    public class World : IDisposable
    {
        private static World _instance;

        public static World Instance
        {
            get
            {
                return _instance ??= new World();
            }
        }

        public GameObject Root { get; set; }

        private readonly Stack<Type> _stack = new();
        private readonly Dictionary<Type, ASingleton> _singletons = new();

        private readonly Dictionary<string, Scene> _scenes = new();

        public Scene GetScene(string sceneName)
        {
            return _scenes.TryGetValue(sceneName, out var scene) ? scene : null;
        }

        public void Dispose()
        {
            _instance = null;

            lock (this)
            {
                while (_stack.Count > 0)
                {
                    Type type = _stack.Pop();
                    if (_singletons.Remove(type, out ASingleton singleton))
                    {
                        singleton.Dispose();
                    }
                }

                // dispose剩下的singleton，主要为了把instance置空
                foreach (var kv in _singletons)
                {
                    kv.Value.Dispose();
                }
            }
        }

        public Scene AddScene(string sceneName, Scene scene)
        {

            if (_scenes.ContainsKey(sceneName))
            {
                throw new Exception($"scene {sceneName} already exists");
            }
            //scene awake可能调用world获得scene,所以先加入再awake
            _scenes.Add(sceneName, scene);

            scene.Awake();
            return scene;
        }

        public void RemoveScene(string sceneName)
        {
            if (_scenes.TryGetValue(sceneName, out var scene))
            {

                scene.Dispose();
                _scenes.Remove(sceneName);
            }
        }


        public T AddSingleton<T>() where T : ASingleton, ISingletonAwake, new()
        {
            T singleton = new();
            singleton.Awake();

            AddSingleton(singleton);
            return singleton;
        }

        public T AddSingleton<T, A>(A a) where T : ASingleton, ISingletonAwake<A>, new()
        {
            T singleton = new();
            singleton.Awake(a);

            AddSingleton(singleton);
            return singleton;
        }

        public T AddSingleton<T, A, B>(A a, B b) where T : ASingleton, ISingletonAwake<A, B>, new()
        {
            T singleton = new();
            singleton.Awake(a, b);

            AddSingleton(singleton);
            return singleton;
        }

        public T AddSingleton<T, A, B, C>(A a, B b, C c) where T : ASingleton, ISingletonAwake<A, B, C>, new()
        {
            T singleton = new();
            singleton.Awake(a, b, c);

            AddSingleton(singleton);
            return singleton;
        }

        public void AddSingleton(ASingleton singleton)
        {
            lock (this)
            {
                Type type = singleton.GetType();
                if (singleton is ISingletonReverseDispose)
                {
                    this._stack.Push(type);
                }
                _singletons[type] = singleton;
            }

            singleton.Register();
        }

    }
}