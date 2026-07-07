using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameEntity.Unity.Framework
{
    public sealed class UGUIView : IUIView
    {
        private InstanceRef _instanceRef;
        private Canvas _canvas;

        internal UGUIView(InstanceRef instanceRef)
        {
            _instanceRef = instanceRef ?? throw new FrameworkException("UGUIView 需要 InstanceRef。");
            GameObject = instanceRef.GameObject;
            Transform = instanceRef.Transform;
            _canvas = EnsureCanvas(GameObject);
        }

        public GameObject GameObject { get; }
        public Transform Transform { get; }
        public Canvas Canvas => _canvas;

        public void SetVisible(bool visible)
        {
            if (GameObject != null)
            {
                GameObject.SetActive(visible);
            }
        }

        public void SetDepth(int depth)
        {
            _canvas = EnsureCanvas(GameObject);
            if (_canvas == null)
            {
                return;
            }

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = depth;
        }

        internal void Release()
        {
            _instanceRef?.Return();
            _instanceRef = null;
        }

        private static Canvas EnsureCanvas(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
            }

            if (go.GetComponent<GraphicRaycaster>() == null)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }
    }

}
