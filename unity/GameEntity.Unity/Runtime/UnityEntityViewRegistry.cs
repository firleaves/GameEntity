using System.Collections.Generic;
using UnityEngine;

namespace GameEntity.Unity
{
    /// <summary>
    /// 维护 Entity 与 Unity GameObject / ComponentView 的映射。
    /// </summary>
    public sealed class UnityEntityViewRegistry : IEntityTreeObserver
    {
        private readonly Dictionary<Entity, ComponentView> _views = new Dictionary<Entity, ComponentView>();
        private readonly Transform _root;

        public static UnityEntityViewRegistry Active { get; internal set; }

        public bool AutoCreateViews { get; }

        public bool DestroyViewsOnEntityDispose { get; }

        public int ViewCount => _views.Count;

        public string LastError { get; private set; }

        public UnityEntityViewRegistry(Transform root, bool autoCreateViews, bool destroyViewsOnEntityDispose)
        {
            _root = root;
            AutoCreateViews = autoCreateViews;
            DestroyViewsOnEntityDispose = destroyViewsOnEntityDispose;
        }

        public ComponentView GetView(Entity entity)
        {
            TryGetView(entity, out ComponentView view);
            return view;
        }

        public bool TryGetView(Entity entity, out ComponentView view)
        {
            if (entity == null)
            {
                view = null;
                return false;
            }

            return _views.TryGetValue(entity, out view);
        }

        public ComponentView Bind(Entity entity, GameObject gameObject)
        {
            if (entity == null)
            {
                LastError = "Cannot bind null entity.";
                return null;
            }

            if (gameObject == null)
            {
                gameObject = new GameObject(CreateViewName(entity));
            }

            ComponentView view = gameObject.GetComponent<ComponentView>();
            if (view == null)
            {
                view = gameObject.AddComponent<ComponentView>();
            }

            view.Bind(entity);
            _views[entity] = view;
            AttachToParent(entity, view);
            return view;
        }

        public void Unbind(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!_views.TryGetValue(entity, out ComponentView view))
            {
                return;
            }

            _views.Remove(entity);
            if (view == null)
            {
                return;
            }

            view.MarkReleased();
            if (DestroyViewsOnEntityDispose)
            {
                Object.Destroy(view.gameObject);
            }
        }

        public void OnEntityRegistered(Entity entity)
        {
            if (!AutoCreateViews || entity == null)
            {
                return;
            }

            EnsureView(entity);
        }

        public void OnEntityParentChanged(Entity entity, Entity oldParent, Entity newParent)
        {
            if (entity == null)
            {
                return;
            }

            ComponentView view = AutoCreateViews ? EnsureView(entity) : GetView(entity);
            if (view != null)
            {
                AttachToParent(entity, view);
            }
        }

        public void OnEntityDisposed(Entity entity)
        {
            Unbind(entity);
        }

        private ComponentView EnsureView(Entity entity)
        {
            if (entity == null)
            {
                return null;
            }

            if (_views.TryGetValue(entity, out ComponentView existing))
            {
                return existing;
            }

            return Bind(entity, new GameObject(CreateViewName(entity)));
        }

        private void AttachToParent(Entity entity, ComponentView view)
        {
            Transform parent = _root;
            Entity parentEntity = entity.Parent;
            if (parentEntity != null)
            {
                ComponentView parentView = AutoCreateViews ? EnsureView(parentEntity) : GetView(parentEntity);
                if (parentView != null)
                {
                    parent = parentView.transform;
                }
                else
                {
                    LastError = $"Parent view missing: {parentEntity.GetType().FullName}";
                }
            }

            view.transform.SetParent(parent, worldPositionStays: false);
        }

        private static string CreateViewName(Entity entity)
        {
            string viewName = entity.GetViewName();
            return string.IsNullOrEmpty(viewName) ? entity.GetType().Name : viewName;
        }
    }
}
