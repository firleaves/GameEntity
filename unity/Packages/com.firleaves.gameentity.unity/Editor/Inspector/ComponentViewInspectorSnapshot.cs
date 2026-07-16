using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity.Unity.Editor
{
    internal enum EntityRole
    {
        Scene,
        Entity,
        Component,
        Unknown
    }

    internal readonly struct InterfaceTag
    {
        public InterfaceTag(string label, bool isState = false, bool isWarning = false)
        {
            Label = label;
            IsState = isState;
            IsWarning = isWarning;
        }

        public string Label { get; }

        public bool IsState { get; }

        public bool IsWarning { get; }
    }

    internal sealed class ComponentViewInspectorSnapshot
    {
        private ComponentViewInspectorSnapshot(
            string name,
            EntityRole role,
            string fullType,
            long id,
            long instanceId,
            bool isReleased,
            bool isDestroyed,
            IReadOnlyList<InterfaceTag> interfaces)
        {
            Name = name;
            Role = role;
            FullType = fullType;
            Id = id;
            InstanceId = instanceId;
            IsReleased = isReleased;
            IsDestroyed = isDestroyed;
            Interfaces = interfaces;
        }

        public string Name { get; }

        public EntityRole Role { get; }

        public string FullType { get; }

        public long Id { get; }

        public long InstanceId { get; }

        public bool IsReleased { get; }

        public bool IsDestroyed { get; }

        public IReadOnlyList<InterfaceTag> Interfaces { get; }

        public static ComponentViewInspectorSnapshot From(ComponentView componentView, Entity entity)
        {
            if (entity == null)
            {
                return new ComponentViewInspectorSnapshot(
                    "Unbound",
                    EntityRole.Unknown,
                    "-",
                    0,
                    componentView == null ? 0 : componentView.InstanceId,
                    componentView != null && componentView.IsReleased,
                    false,
                    Array.Empty<InterfaceTag>());
            }

            Type entityType = entity.GetType();
            EntityRole role = EntityRoleResolver.Resolve(entity);
            return new ComponentViewInspectorSnapshot(
                ResolveName(entity, entityType, role),
                role,
                entityType.FullName ?? entityType.Name,
                entity.Id,
                entity.InstanceId,
                componentView != null && componentView.IsReleased,
                entity.IsDestroyed,
                InterfaceTagResolver.Resolve(entity));
        }

        private static string ResolveName(Entity entity, Type entityType, EntityRole role)
        {
            if (role == EntityRole.Component && entity.Parent != null)
            {
                return $"{SimplifyName(entity.Parent.GetViewName(), entity.Parent.GetType())}.{entityType.Name}";
            }

            if (entity is Scene scene && !string.IsNullOrWhiteSpace(scene.Name))
            {
                return scene.Name;
            }

            return SimplifyName(entity.GetViewName(), entityType);
        }

        private static string SimplifyName(string viewName, Type fallbackType)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return fallbackType.Name;
            }

            string fallbackFullName = fallbackType.FullName ?? fallbackType.Name;
            if (viewName == fallbackFullName)
            {
                return fallbackType.Name;
            }

            return viewName;
        }
    }

    internal static class EntityRoleResolver
    {
        public static EntityRole Resolve(Entity entity)
        {
            if (entity == null)
            {
                return EntityRole.Unknown;
            }

            if (entity is Scene)
            {
                return EntityRole.Scene;
            }

            Entity parent = entity.Parent;
            if (parent == null)
            {
                return EntityRole.Unknown;
            }

            if (parent.ComponentsCount() > 0 &&
                parent.Components.Any(component => ReferenceEquals(component, entity)))
            {
                return EntityRole.Component;
            }

            if (parent.ChildrenCount() > 0 &&
                parent.Children.Any(child => ReferenceEquals(child, entity)))
            {
                return EntityRole.Entity;
            }

            return EntityRole.Unknown;
        }

        public static string ToLabel(EntityRole role)
        {
            switch (role)
            {
                case EntityRole.Scene:
                    return "Scene";
                case EntityRole.Entity:
                    return "Entity";
                case EntityRole.Component:
                    return "Component";
                default:
                    return "Unknown";
            }
        }
    }

    internal static class InterfaceTagResolver
    {
        public static IReadOnlyList<InterfaceTag> Resolve(Entity entity)
        {
            if (entity == null)
            {
                return Array.Empty<InterfaceTag>();
            }

            List<InterfaceTag> tags = new List<InterfaceTag>();
            Type type = entity.GetType();

            if (ImplementsGeneric(type, typeof(IAwake<>)) ||
                ImplementsGeneric(type, typeof(IAwake<,>)) ||
                ImplementsGeneric(type, typeof(IAwake<,,>)) ||
                ImplementsGeneric(type, typeof(IAwake<,,,>)))
            {
                tags.Add(new InterfaceTag("IAwake<T>"));
            }
            else if (entity is IAwake)
            {
                tags.Add(new InterfaceTag("IAwake"));
            }

            AddIf(tags, entity is IFixedUpdate, "IFixedUpdate");
            AddIf(tags, entity is IUpdate, "IUpdate");
            AddIf(tags, entity is IStart, "IStart");
            AddIf(tags, entity is IDestroy, "IDestroy");
            AddIf(tags, entity is IEntityUpdateInterval, "IEntityUpdateInterval");
            AddIf(tags, entity is Scene, "Scene");

            if (entity is IEntityReadyState readyState)
            {
                tags.Add(new InterfaceTag("IEntityReadyState"));
                tags.Add(ResolveBooleanState(
                    () => readyState.IsReady,
                    "Ready",
                    "Not Ready",
                    "Ready State Error"));
            }

            if (entity is IEntityUpdateState updateState)
            {
                tags.Add(new InterfaceTag("IEntityUpdateState"));
                tags.Add(ResolveBooleanState(
                    () => updateState.IsUpdateEnabled,
                    "Update Enabled",
                    "Update Disabled",
                    "Update State Error"));
            }

            if (entity is IStart && entity.GetWorld().Hierarchy.TryGetNode(entity, out var record))
            {
                tags.Add(new InterfaceTag(
                    record.IsStartFaulted ? "Start Faulted" : record.IsStarted ? "Started" : "Waiting to Start",
                    isState: true,
                    isWarning: record.IsStartFaulted));
            }

            UpdateRequirementResult requirementResult = UpdateRequirementResolver.Check(entity);
            if (requirementResult.HasRequirements)
            {
                tags.Add(new InterfaceTag("RequireForUpdate"));
                tags.Add(CreateRequirementStateTag(requirementResult));
            }

            return tags;
        }

        private static InterfaceTag CreateRequirementStateTag(UpdateRequirementResult result)
        {
            string requirementName = result.RequirementType?.Name ?? "Owner";
            switch (result.BlockReason)
            {
                case UpdateRequirementBlockReason.ComponentMissing:
                    return new InterfaceTag($"Missing {requirementName}", isState: true, isWarning: true);
                case UpdateRequirementBlockReason.ComponentNotReady:
                    return new InterfaceTag($"Waiting for {requirementName}", isState: true, isWarning: true);
                case UpdateRequirementBlockReason.ComponentStateError:
                    return new InterfaceTag($"State error: {requirementName}", isState: true, isWarning: true);
                case UpdateRequirementBlockReason.NotComponent:
                    return new InterfaceTag("Invalid update requirement target", isState: true, isWarning: true);
                case UpdateRequirementBlockReason.OwnerMissing:
                    return new InterfaceTag("Update requirement owner missing", isState: true, isWarning: true);
                default:
                    return new InterfaceTag("Update Requirements Ready", isState: true);
            }
        }

        private static InterfaceTag ResolveBooleanState(
            Func<bool> readState,
            string trueLabel,
            string falseLabel,
            string errorLabel)
        {
            try
            {
                bool value = readState();
                return new InterfaceTag(
                    value ? trueLabel : falseLabel,
                    isState: true,
                    isWarning: !value);
            }
            catch
            {
                return new InterfaceTag(errorLabel, isState: true, isWarning: true);
            }
        }

        private static void AddIf(List<InterfaceTag> tags, bool condition, string label)
        {
            if (condition)
            {
                tags.Add(new InterfaceTag(label));
            }
        }

        private static bool ImplementsGeneric(Type type, Type genericInterfaceType)
        {
            if (type == null)
            {
                return false;
            }

            return type.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == genericInterfaceType);
        }
    }
}
