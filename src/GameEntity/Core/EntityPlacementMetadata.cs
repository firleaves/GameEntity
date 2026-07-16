using System;
using System.Collections.Concurrent;
using System.Linq;

namespace GameEntity
{
    internal readonly struct EntityPlacementConstraint
    {
        public EntityPlacementConstraint(bool childOnly, Type parentType)
        {
            ChildOnly = childOnly;
            ParentType = parentType;
        }

        public bool ChildOnly { get; }

        public Type ParentType { get; }
    }

    internal static class EntityPlacementMetadata
    {
        private static readonly ConcurrentDictionary<Type, EntityPlacementConstraint> Cache =
            new ConcurrentDictionary<Type, EntityPlacementConstraint>();

        public static void ValidateChild(Entity owner, Type entityType)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            EntityPlacementConstraint constraint = GetConstraint(entityType);
            if (typeof(Scene).IsAssignableFrom(entityType))
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} derives from Scene and can only be registered as a SceneRoot through World.AddScene.");
            }

            if (!constraint.ChildOnly || constraint.ParentType == null)
            {
                return;
            }

            if (!constraint.ParentType.IsInstanceOfType(owner))
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} declares ChildOf({constraint.ParentType.FullName}) " +
                    $"and cannot be attached to {owner.GetType().FullName}.");
            }
        }

        public static void ValidateComponent(Type entityType)
        {
            EntityPlacementConstraint constraint = GetConstraint(entityType);
            if (typeof(Scene).IsAssignableFrom(entityType))
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} derives from Scene and cannot be attached as a Component.");
            }

            if (constraint.ChildOnly)
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} declares ChildOf and cannot be attached as a Component.");
            }
        }

        public static void ValidateSceneRoot(Type entityType)
        {
            EntityPlacementConstraint constraint = GetConstraint(entityType);
            if (!typeof(Scene).IsAssignableFrom(entityType))
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} does not derive from Scene and cannot be registered as a SceneRoot.");
            }

            if (constraint.ChildOnly)
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} declares ChildOf and cannot be registered as a SceneRoot.");
            }
        }

        private static EntityPlacementConstraint GetConstraint(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            return Cache.GetOrAdd(entityType, BuildConstraint);
        }

        private static EntityPlacementConstraint BuildConstraint(Type entityType)
        {
            ChildOfAttribute attribute = entityType
                .GetCustomAttributes(typeof(ChildOfAttribute), true)
                .Cast<ChildOfAttribute>()
                .FirstOrDefault();
            if (attribute == null)
            {
                return default;
            }

            if (attribute.Type != null && !typeof(Entity).IsAssignableFrom(attribute.Type))
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} ChildOf target must derive from Entity: {attribute.Type.FullName}.");
            }

            return new EntityPlacementConstraint(childOnly: true, attribute.Type);
        }
    }
}
