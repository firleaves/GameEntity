using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal static class UpdateRequirementMetadata
    {
        private static readonly ConcurrentDictionary<Type, Type[]> Cache = new ConcurrentDictionary<Type, Type[]>();

        public static Type[] GetRequirementTypes(Type entityType)
        {
            if (entityType == null)
            {
                return Array.Empty<Type>();
            }

            return Cache.GetOrAdd(entityType, BuildRequirementTypes);
        }

        public static void ValidateNoCycles(Type entityType)
        {
            if (!TryGetCycle(entityType, out Type[] cycle))
            {
                return;
            }

            string cyclePath = string.Join(" -> ", cycle.Select(type => type.FullName));
            throw new InvalidOperationException($"{entityType.FullName} has a RequireForUpdate cycle: {cyclePath}.");
        }

        public static bool TryGetCycle(Type entityType, out Type[] cycle)
        {
            cycle = Array.Empty<Type>();
            if (entityType == null)
            {
                return false;
            }

            var completed = new HashSet<Type>();
            var activeIndices = new Dictionary<Type, int>();
            var path = new List<Type>();
            return TryVisit(entityType, completed, activeIndices, path, out cycle);
        }

        private static Type[] BuildRequirementTypes(Type entityType)
        {
            var attributes = entityType
                .GetCustomAttributes(typeof(RequireForUpdateAttribute), true)
                .Cast<RequireForUpdateAttribute>()
                .ToArray();

            if (attributes.Length == 0)
            {
                return Array.Empty<Type>();
            }

            var requirementTypes = new List<Type>();
            foreach (var attribute in attributes)
            {
                if (attribute.RequiredComponentTypes == null || attribute.RequiredComponentTypes.Length == 0)
                {
                    throw new InvalidOperationException($"{entityType.FullName} has an empty RequireForUpdate declaration.");
                }

                foreach (var requirementType in attribute.RequiredComponentTypes)
                {
                    ValidateRequirementType(entityType, requirementType);
                    if (!requirementTypes.Contains(requirementType))
                    {
                        requirementTypes.Add(requirementType);
                    }
                }
            }

            return requirementTypes.ToArray();
        }

        private static void ValidateRequirementType(Type entityType, Type requirementType)
        {
            if (requirementType == null)
            {
                throw new InvalidOperationException($"{entityType.FullName} has a null RequireForUpdate component type.");
            }

            if (!typeof(Entity).IsAssignableFrom(requirementType))
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} RequireForUpdate target must derive from Entity: {requirementType.FullName}.");
            }

            if (requirementType == entityType)
            {
                throw new InvalidOperationException($"{entityType.FullName} cannot RequireForUpdate itself.");
            }

            if (requirementType.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"{entityType.FullName} RequireForUpdate target must be an exact, non-abstract Component type: {requirementType.FullName}.");
            }
        }

        private static bool TryVisit(
            Type entityType,
            HashSet<Type> completed,
            Dictionary<Type, int> activeIndices,
            List<Type> path,
            out Type[] cycle)
        {
            if (activeIndices.TryGetValue(entityType, out int cycleStartIndex))
            {
                cycle = path.Skip(cycleStartIndex).Concat(new[] { entityType }).ToArray();
                return true;
            }

            if (completed.Contains(entityType))
            {
                cycle = Array.Empty<Type>();
                return false;
            }

            activeIndices.Add(entityType, path.Count);
            path.Add(entityType);
            foreach (Type requirementType in GetRequirementTypes(entityType))
            {
                if (TryVisit(requirementType, completed, activeIndices, path, out cycle))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            activeIndices.Remove(entityType);
            completed.Add(entityType);
            cycle = Array.Empty<Type>();
            return false;
        }
    }
}
