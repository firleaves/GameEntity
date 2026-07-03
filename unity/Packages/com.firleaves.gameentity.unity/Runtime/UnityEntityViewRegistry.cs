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

        public bool DestroyViewsOnEntityDestroy { get; }

        public int ViewCount => _views.Count;

        public string LastError { get; private set; }

        public UnityEntityViewRegistry(Transform root, bool autoCreateViews, bool destroyViewsOnEntityDestroy)
        {
            _root = root;
            AutoCreateViews = autoCreateViews;
            DestroyViewsOnEntityDestroy = destroyViewsOnEntityDestroy;
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

            if (!_views.TryGetValue(entity, out view))
            {
                return false;
            }

            if (view != null)
            {
                return true;
            }

            _views.Remove(entity);
            view = null;
            return false;
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

            if (_views.TryGetValue(entity, out ComponentView existing) && existing != null && existing.gameObject != gameObject)
            {
                Unbind(entity);
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
            if (DestroyViewsOnEntityDestroy)
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

        public void OnEntityDestroyed(Entity entity)
        {
            Unbind(entity);
        }

        private ComponentView EnsureView(Entity entity)
        {
            if (entity == null)
            {
                return null;
            }

            if (TryGetView(entity, out ComponentView existing))
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
