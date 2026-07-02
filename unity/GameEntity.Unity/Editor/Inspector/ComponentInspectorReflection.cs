using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameEntity.Unity.Editor
{
    internal static class ComponentInspectorReflection
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();

        private static readonly HashSet<string> HiddenRelationshipMembers = new HashSet<string>
        {
            "Parent",
            "Children",
            "Components",
            "_parent",
            "_children",
            "_components"
        };

        public static IEnumerable<FieldInfo> GetInspectableFields(Type type)
        {
            if (type == null)
            {
                return Array.Empty<FieldInfo>();
            }

            if (!FieldCache.TryGetValue(type, out FieldInfo[] fields))
            {
                List<FieldInfo> list = new List<FieldInfo>();
                for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
                {
                    list.AddRange(current.GetFields(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly));
                }

                fields = list.ToArray();
                FieldCache[type] = fields;
            }

            return fields;
        }

        public static IEnumerable<PropertyInfo> GetInspectableProperties(Type type)
        {
            if (type == null)
            {
                return Array.Empty<PropertyInfo>();
            }

            if (PropertyCache.TryGetValue(type, out PropertyInfo[] cachedProperties))
            {
                return cachedProperties;
            }

            HashSet<string> seen = new HashSet<string>();
            List<PropertyInfo> properties = new List<PropertyInfo>();
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                PropertyInfo[] declaredProperties = current.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

                foreach (PropertyInfo property in declaredProperties)
                {
                    if (!seen.Add(property.Name))
                    {
                        continue;
                    }

                    properties.Add(property);
                }
            }

            cachedProperties = properties.ToArray();
            PropertyCache[type] = cachedProperties;
            return cachedProperties;
        }

        public static IReadOnlyList<ComponentMemberDescriptor> GetInspectableMembers(
            object target,
            bool showPrivate,
            bool includeProperties,
            bool includeRawFrameworkFields = false)
        {
            if (target == null)
            {
                return Array.Empty<ComponentMemberDescriptor>();
            }

            List<ComponentMemberDescriptor> members = new List<ComponentMemberDescriptor>();
            Type entityType = target.GetType();
            foreach (FieldInfo field in GetInspectableFields(entityType))
            {
                if (ShouldSkipField(field, showPrivate, includeRawFrameworkFields))
                {
                    continue;
                }

                object value = null;
                Exception readException = null;
                try
                {
                    value = field.GetValue(target);
                }
                catch (Exception e)
                {
                    readException = e;
                }

                members.Add(new ComponentMemberDescriptor(
                    ComponentMemberKind.Field,
                    GetDisplayName(field.Name),
                    field.Name,
                    field.DeclaringType,
                    field.FieldType,
                    value,
                    readException,
                    $"field.{field.DeclaringType?.FullName}.{field.Name}"));
            }

            if (!includeProperties)
            {
                return members;
            }

            foreach (PropertyInfo property in GetInspectableProperties(entityType))
            {
                if (ShouldSkipProperty(property, showPrivate))
                {
                    continue;
                }

                object value = null;
                Exception readException = null;
                try
                {
                    value = property.GetValue(target, null);
                }
                catch (Exception e)
                {
                    readException = e;
                }

                members.Add(new ComponentMemberDescriptor(
                    ComponentMemberKind.Property,
                    GetDisplayName(property.Name),
                    property.Name,
                    property.DeclaringType,
                    property.PropertyType,
                    value,
                    readException,
                    $"property.{property.DeclaringType?.FullName}.{property.Name}"));
            }

            return members;
        }

        public static bool ShouldSkipField(FieldInfo field, bool showPrivate, bool includeRawFrameworkFields = false)
        {
            if (field == null || field.IsStatic || field.IsLiteral)
            {
                return true;
            }

            if (!showPrivate && !field.IsPublic)
            {
                return true;
            }

            if (field.DeclaringType == typeof(Entity) && !includeRawFrameworkFields)
            {
                return true;
            }

            if (IsHiddenRelationshipMember(field.Name) && !includeRawFrameworkFields)
            {
                return true;
            }

            if (IsCompilerBackingField(field.Name))
            {
                return true;
            }

            if (field.IsDefined(typeof(NonSerializedAttribute), inherit: true) ||
                field.IsDefined(typeof(HideInInspector), inherit: true) ||
                field.IsDefined(typeof(GameEntityInspectorIgnoreAttribute), inherit: true))
            {
                return true;
            }

            BrowsableAttribute browsable = field.GetCustomAttribute<BrowsableAttribute>();
            return browsable != null && !browsable.Browsable;
        }

        public static bool ShouldSkipProperty(PropertyInfo property, bool showPrivate)
        {
            if (property == null || !property.CanRead || property.GetIndexParameters().Length > 0)
            {
                return true;
            }

            MethodInfo getter = property.GetGetMethod(nonPublic: true);
            if (getter == null || getter.IsStatic)
            {
                return true;
            }

            if (!showPrivate && !getter.IsPublic)
            {
                return true;
            }

            if (property.DeclaringType == typeof(Entity))
            {
                return true;
            }

            if (IsHiddenRelationshipMember(property.Name))
            {
                return true;
            }

            if (property.IsDefined(typeof(HideInInspector), inherit: true) ||
                property.IsDefined(typeof(GameEntityInspectorIgnoreAttribute), inherit: true))
            {
                return true;
            }

            BrowsableAttribute browsable = property.GetCustomAttribute<BrowsableAttribute>();
            return browsable != null && !browsable.Browsable;
        }

        public static string GetDisplayName(string memberName)
        {
            if (string.IsNullOrEmpty(memberName))
            {
                return memberName;
            }

            return ObjectNames.NicifyVariableName(memberName);
        }

        public static bool MatchesSearch(string searchText, string displayName, string rawName, Type type, object value)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string query = searchText.Trim();
            return Contains(displayName, query) ||
                   Contains(rawName, query) ||
                   Contains(type?.Name, query) ||
                   Contains(value?.ToString(), query);
        }

        private static bool IsHiddenRelationshipMember(string name)
        {
            return HiddenRelationshipMembers.Contains(name);
        }

        private static bool IsCompilerBackingField(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.StartsWith("<", StringComparison.Ordinal) &&
                   name.Contains("k__BackingField");
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
