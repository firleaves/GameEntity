using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace GE
{
    /// <summary>
    /// 标记字段在 ComponentView 中跳过显示
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SkipInComponentViewAttribute : Attribute
    {
        public string Reason { get; }

        public SkipInComponentViewAttribute(string reason = "")
        {
            Reason = reason;
        }
    }
    [CustomEditor(typeof(ComponentView))]
    internal class ComponentViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ComponentView componentView = (ComponentView)target;
            Entity component = componentView.Component;
            ComponentViewHelper.Draw(component);
        }
    }

    public static class ComponentViewHelper
    {
        private static readonly List<ITypeDrawer> typeDrawers = new List<ITypeDrawer>();
        private static readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        static ComponentViewHelper()
        {
            Assembly assembly = typeof(ComponentViewHelper).Assembly;
            foreach (Type type in assembly.GetTypes())
            {
                if (!type.IsDefined(typeof(TypeDrawerAttribute)))
                {
                    continue;
                }

                ITypeDrawer iTypeDrawer = (ITypeDrawer)Activator.CreateInstance(type);
                typeDrawers.Add(iTypeDrawer);
            }
        }

        public static void Draw(Entity entity)
        {
            try
            {
                Type entityType = entity.GetType();
                FieldInfo[] fields = entityType
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                PropertyInfo[] properties = entityType
                        .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                EditorGUILayout.BeginVertical();

                EditorGUILayout.LongField("InstanceId: ", entity.InstanceId);
                EditorGUILayout.LongField("Id: ", entity.Id);

                // 绘制字段
                foreach (FieldInfo fieldInfo in fields)
                {
                    Type type = fieldInfo.FieldType;

                    // 跳过 HideInInspector 标记的字段
                    if (type.IsDefined(typeof(HideInInspector), false) || fieldInfo.IsDefined(typeof(HideInInspector), false))
                    {
                        continue;
                    }

                    // 跳过 Entity 基类的内部变量
                    if (ShouldSkipField(fieldInfo))
                    {
                        continue;
                    }

                    object value = fieldInfo.GetValue(entity);

                    // 优先使用 TypeDrawer（包含 List/Dictionary/基础类型/UnityObject 等）
                    string fieldName = GetPrettyMemberName(fieldInfo.Name);
                    if (TryDrawWithTypeDrawers(type, fieldName, value, entity, out object newValue))
                    {
                        if (!Equals(newValue, value))
                        {
                            fieldInfo.SetValue(entity, newValue);
                        }
                        continue;
                    }

                    // 处理类对象类型（未被任何 TypeDrawer 接管）
                    if (type.IsClass && type != typeof(string) && type != typeof(UnityEngine.Object))
                    {
                        DrawClassField(fieldInfo, value, entity);
                        continue;
                    }
                }

                // 绘制属性
                foreach (PropertyInfo propInfo in properties)
                {
                    if (!propInfo.CanRead)
                        continue;
                    if (ShouldSkipProperty(propInfo))
                        continue;

                    Type type = propInfo.PropertyType;
                    object value;
                    try { value = propInfo.GetValue(entity); }
                    catch { continue; }

                    string propName = GetPrettyMemberName(propInfo.Name);

                    // 先尝试通过 TypeDrawer 绘制（包含容器、基础类型、UnityObject 等）
                    if (TryDrawWithTypeDrawers(type, propName, value, entity, out object propNewValue))
                    {
                        if (!Equals(propNewValue, value) && propInfo.CanWrite)
                        {
                            propInfo.SetValue(entity, propNewValue);
                        }
                        continue;
                    }

                    // 类对象属性（未被任何 TypeDrawer 接管）
                    if (type.IsClass && type != typeof(string) && type != typeof(UnityEngine.Object))
                    {
                        DrawClassProperty(propInfo, value, entity);
                        continue;
                    }
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log($"component view error: {entity.GetType().FullName} {e}");
            }
        }

        /// <summary>
        /// 判断是否应该跳过某个字段
        /// </summary>
        private static bool ShouldSkipField(FieldInfo fieldInfo)
        {
            string fieldName = fieldInfo.Name;

            if (fieldInfo.DeclaringType == typeof(Entity))
            {
                return true;
            }

            string[] skipFieldNames = {
                "_instanceId",
                "_id",
                "_components",
                "_children",
                "_parent",
                "_isActive",
                "_isDestroyed",
                "_updateStrategy",
                "_updateCount",
                "_lastUpdateTime",
                "_poolIndex",
                "_isFromPool",
                "_systemData",
                "_debugInfo",
                "_internalState",
                "_cachedData",
                "_tempBuffer",
                "_eventHandlers",
                "_coroutines",
                "_asyncTasks"
            };

            if (Array.Exists(skipFieldNames, name => string.Equals(fieldName, name, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (fieldName.StartsWith("_") && fieldName.Contains("Internal"))
            {
                return true;
            }

            string[] skipKeywords = { "Internal", "Private", "System", "Backing" };
            foreach (string keyword in skipKeywords)
            {
                if (fieldName.Contains(keyword))
                {
                    return true;
                }
            }

            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // 可以选择跳过字典类型，因为它们通常很大
                // return true;
            }

            if (fieldInfo.IsDefined(typeof(System.ComponentModel.BrowsableAttribute), false))
            {
                var browsableAttr = fieldInfo.GetCustomAttribute<System.ComponentModel.BrowsableAttribute>();
                if (browsableAttr != null && !browsableAttr.Browsable)
                {
                    return true;
                }
            }

            if (fieldInfo.IsDefined(typeof(SkipInComponentViewAttribute), false))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断是否应该跳过某个属性
        /// </summary>
        private static bool ShouldSkipProperty(PropertyInfo propInfo)
        {
            string name = propInfo.Name;

            // 索引器跳过
            if (propInfo.GetIndexParameters().Length > 0)
                return true;

            // 来自 Entity 基类的属性跳过
            if (propInfo.DeclaringType == typeof(Entity))
                return true;

            // HideInInspector
            if (propInfo.IsDefined(typeof(HideInInspector), false))
                return true;

            // Browsable(false)
            var browsableAttr = propInfo.GetCustomAttribute<System.ComponentModel.BrowsableAttribute>();
            if (browsableAttr != null && !browsableAttr.Browsable)
                return true;

            // 名称包含内部关键字
            string[] skipKeywords = { "Internal", "Private", "System", "Backing" };
            foreach (string keyword in skipKeywords)
            {
                if (name.Contains(keyword))
                    return true;
            }

            return false;
        }

        private static bool IsList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool IsDictionary(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        private static void DrawClassField(FieldInfo fieldInfo, object value, Entity entity)
        {
            string fieldName = GetPrettyMemberName(fieldInfo.Name);

            // 检查是否为null
            if (value == null)
            {
                EditorGUILayout.LabelField($"{fieldName}: null");
                return;
            }

            // 创建可折叠的标题
            bool isExpanded = EditorGUILayout.Foldout(GetFoldoutState(fieldName), fieldName, true);
            SetFoldoutState(fieldName, isExpanded);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                try
                {
                    // 递归绘制类对象的字段
                    DrawObjectFields(value, fieldName);
                }
                catch (Exception e)
                {
                    EditorGUILayout.LabelField($"Error drawing {fieldName}: {e.Message}");
                }

                EditorGUI.indentLevel--;
            }
        }

        private static void DrawClassProperty(PropertyInfo propInfo, object value, Entity entity)
        {
            string propName = GetPrettyMemberName(propInfo.Name);

            if (value == null)
            {
                EditorGUILayout.LabelField($"{propName}: null");
                return;
            }

            bool isExpanded = EditorGUILayout.Foldout(GetFoldoutState(propName), propName, true);
            SetFoldoutState(propName, isExpanded);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                try
                {
                    DrawObjectFields(value, propName);
                }
                catch (Exception e)
                {
                    EditorGUILayout.LabelField($"Error drawing {propName}: {e.Message}");
                }
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawObjectFields(object obj, string parentPath)
        {
            if (obj == null) return;

            Type objType = obj.GetType();

            // 跳过一些特殊类型
            if (objType == typeof(string) || objType == typeof(UnityEngine.Object) ||
                objType.IsEnum || objType.IsPrimitive || objType.IsValueType)
            {
                return;
            }

            FieldInfo[] fields = objType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(HideInInspector), false))
                    continue;

                string fieldName = GetPrettyMemberName(field.Name);

                object fieldValue = field.GetValue(obj);
                string fullPath = $"{parentPath}.{fieldName}";

                // 优先用 TypeDrawer（包含容器/基础类型/UnityObject 等）
                if (TryDrawWithTypeDrawers(field.FieldType, fieldName, fieldValue, obj, out object newValue))
                {
                    if (!Equals(newValue, fieldValue))
                    {
                        field.SetValue(obj, newValue);
                    }
                    continue;
                }

                // 处理嵌套的类对象
                if (field.FieldType.IsClass && field.FieldType != typeof(string) && field.FieldType != typeof(UnityEngine.Object))
                {
                    if (fieldValue == null)
                    {
                        EditorGUILayout.LabelField($"{fieldName}: null");
                    }
                    else
                    {
                        bool isExpanded = EditorGUILayout.Foldout(GetFoldoutState(fullPath), fieldName, true);
                        SetFoldoutState(fullPath, isExpanded);

                        if (isExpanded)
                        {
                            EditorGUI.indentLevel++;
                            DrawObjectFields(fieldValue, fullPath);
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                else
                {
                    // 其他不可编辑类型，显示名称和值
                    string displayValue = fieldValue?.ToString() ?? "null";
                    EditorGUILayout.LabelField($"{fieldName}: {displayValue} ({field.FieldType.Name})");
                }
            }
        }

        private static string GetPrettyMemberName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            // 自动属性 backing field 形如 <Name>k__BackingField
            if (raw.Length > 17 && raw.Contains("k__BackingField"))
            {
                return raw.Substring(1, raw.Length - 17);
            }
            return raw;
        }

        private static bool TryDrawWithTypeDrawers(Type type, string label, object value, object target, out object newValue)
        {
            foreach (ITypeDrawer typeDrawer in typeDrawers)
            {
                if (!typeDrawer.HandlesType(type))
                    continue;
                try
                {
                    newValue = typeDrawer.DrawAndGetNewValue(type, label, value, target);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    break;
                }
            }
            newValue = value;
            return false;
        }

        private static void DrawListField(FieldInfo field, object listValue, string fieldName, string fullPath)
        {
            if (listValue == null)
            {
                EditorGUILayout.LabelField($"{fieldName}: null");
                return;
            }

            Type listType = field.FieldType;
            Type elementType = listType.GetGenericArguments()[0];

            PropertyInfo countProperty = listType.GetProperty("Count");
            int count = (int)countProperty.GetValue(listValue);

            bool isExpanded = EditorGUILayout.Foldout(GetFoldoutState(fullPath), $"{fieldName} [{count}]", true);
            SetFoldoutState(fullPath, isExpanded);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField($"Count: {count}");

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        object element = listType.GetMethod("get_Item").Invoke(listValue, new object[] { i });
                        string elementPath = $"{fullPath}[{i}]";

                        if (element == null)
                        {
                            EditorGUILayout.LabelField($"[{i}]: null");
                        }
                        else if (elementType.IsPrimitive || elementType == typeof(string) || elementType.IsEnum)
                        {
                            EditorGUILayout.LabelField($"[{i}]: {element}");
                        }
                        else if (elementType.IsClass)
                        {
                            bool elementExpanded = EditorGUILayout.Foldout(GetFoldoutState(elementPath), $"[{i}]: {elementType.Name}", true);
                            SetFoldoutState(elementPath, elementExpanded);

                            if (elementExpanded)
                            {
                                EditorGUI.indentLevel++;
                                DrawObjectFields(element, elementPath);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        EditorGUILayout.LabelField($"[{i}]: Error - {e.Message}");
                    }
                }

                EditorGUI.indentLevel--;
            }
        }



        private static bool GetFoldoutState(string key)
        {
            return foldoutStates.TryGetValue(key, out bool state) && state;
        }

        private static void SetFoldoutState(string key, bool state)
        {
            foldoutStates[key] = state;
        }
    }
}