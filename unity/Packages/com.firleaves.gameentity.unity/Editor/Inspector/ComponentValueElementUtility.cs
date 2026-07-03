using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace GameEntity.Unity.Editor
{
    internal static class ComponentValueElementUtility
    {
        public static bool IsEntityRef(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntityRef<>);
        }

        public static Entity ReadEntityRef(object value, Type type)
        {
            if (value == null || !IsEntityRef(type))
            {
                return null;
            }

            FieldInfo instanceIdField = type.GetField("_instanceId", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo entityField = type.GetField("_entity", BindingFlags.NonPublic | BindingFlags.Instance);
            Entity entity = entityField?.GetValue(value) as Entity;
            if (entity == null)
            {
                return null;
            }

            object instanceIdValue = instanceIdField?.GetValue(value);
            if (instanceIdValue is long instanceId && entity.InstanceId != instanceId)
            {
                return null;
            }

            return entity;
        }

        public static bool IsDictionaryLike(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetCollectionCount(object value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            foreach (Type interfaceType in value.GetType().GetInterfaces())
            {
                if (!interfaceType.IsGenericType)
                {
                    continue;
                }

                Type genericType = interfaceType.GetGenericTypeDefinition();
                if (genericType != typeof(ICollection<>) &&
                    genericType != typeof(IReadOnlyCollection<>))
                {
                    continue;
                }

                PropertyInfo countProperty = interfaceType.GetProperty("Count");
                object count = countProperty?.GetValue(value, null);
                return count is int intCount ? intCount : 0;
            }

            return -1;
        }

        public static IEnumerable<KeyValuePair<object, object>> EnumerateDictionary(object value)
        {
            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    yield return new KeyValuePair<object, object>(entry.Key, entry.Value);
                }

                yield break;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    Type itemType = item.GetType();
                    PropertyInfo keyProperty = itemType.GetProperty("Key");
                    PropertyInfo valueProperty = itemType.GetProperty("Value");
                    if (keyProperty == null || valueProperty == null)
                    {
                        continue;
                    }

                    yield return new KeyValuePair<object, object>(
                        keyProperty.GetValue(item, null),
                        valueProperty.GetValue(item, null));
                }
            }
        }

        public static string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string text)
            {
                return text;
            }

            if (value is Entity entity)
            {
                return $"{entity.GetType().Name} ({entity.Id})";
            }

            if (value is UnityEngine.Object unityObject)
            {
                return unityObject.name;
            }

            return value.ToString();
        }

    }
}
