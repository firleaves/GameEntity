using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GE
{
    [TypeDrawer]
    public class ListTypeDrawer : ITypeDrawer
    {
        private static readonly System.Collections.Generic.Dictionary<string, bool> s_Foldouts = new System.Collections.Generic.Dictionary<string, bool>();

        public bool HandlesType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>);
        }

        public object DrawAndGetNewValue(Type memberType, string memberName, object value, object target)
        {
            Type elementType = memberType.GetGenericArguments()[0];

            // 创建实例
            if (value == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(memberName + ": null");
                if (GUILayout.Button("Create", GUILayout.MaxWidth(70)))
                {
                    value = Activator.CreateInstance(memberType);
                }
                EditorGUILayout.EndHorizontal();
                return value;
            }

            IList list = (IList)value;
            int count = list.Count;

            string foldKey = GetFoldKey(memberName, target);
            bool isExpanded = GetFoldout(foldKey);
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{memberName} [{count}]", true);
            SetFoldout(foldKey, isExpanded);

            if (!isExpanded) return value;

            EditorGUI.indentLevel++;

            // 显示每个元素
            for (int i = 0; i < count; i++)
            {
                object elem = list[i];
                string label = $"[{i}]";

                object newElem = DrawElement(elementType, label, elem);
                if (!Equals(newElem, elem))
                {
                    list[i] = newElem;
                }
            }

            EditorGUI.indentLevel--;

            return value;
        }

        private object DrawElement(Type elementType, string label, object elem)
        {
            // UnityEngine.Object
            if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                return EditorGUILayout.ObjectField(label, (UnityEngine.Object)elem, elementType, true);
            }

            // 基础类型
            if (elementType == typeof(int))
            {
                int v = elem != null ? (int)elem : 0;
                return EditorGUILayout.IntField(label, v);
            }
            if (elementType == typeof(long))
            {
                long v = elem != null ? (long)elem : 0L;
                return EditorGUILayout.LongField(label, v);
            }
            if (elementType == typeof(float))
            {
                float v = elem != null ? (float)elem : 0f;
                return EditorGUILayout.FloatField(label, v);
            }
            if (elementType == typeof(double))
            {
                double v = elem != null ? (double)elem : 0d;
                return EditorGUILayout.DoubleField(label, v);
            }
            if (elementType == typeof(bool))
            {
                bool v = elem != null ? (bool)elem : false;
                return EditorGUILayout.Toggle(label, v);
            }
            if (elementType == typeof(string))
            {
                string v = elem as string ?? string.Empty;
                return EditorGUILayout.TextField(label, v);
            }
            if (elementType.IsEnum)
            {
                Enum v = elem as Enum ?? (Enum)Activator.CreateInstance(elementType);
                return EditorGUILayout.EnumPopup(label, v);
            }

            // Entity：直接展示其 ViewGO
            if (typeof(Entity).IsAssignableFrom(elementType))
            {
                var ent = elem as Entity;
                GameObject go = ent != null ? ent.ViewGO : null;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(label, go, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();
                return elem;
            }

            // EntityRef<T>：展示其指向实体的 ViewGO
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(EntityRef<>))
            {
                if (elem != null)
                {
                    var fi = elementType.GetField("entity", BindingFlags.NonPublic | BindingFlags.Instance);
                    var ent = fi != null ? fi.GetValue(elem) as Entity : null;
                    GameObject go = ent != null ? ent.ViewGO : null;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(label, go, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.LabelField(label, "null");
                }
                return elem;
            }

            // 结构体/自定义类：折叠显示其字段（避免显示容器内置变量）
            string foldKey = GetFoldKey(label, elem);
            bool expanded = GetFoldout(foldKey);
            expanded = EditorGUILayout.Foldout(expanded, $"{label}: {elementType.Name}", true);
            SetFoldout(foldKey, expanded);
            if (expanded)
            {
                EditorGUI.indentLevel++;
                if (elem == null)
                {
                    EditorGUILayout.LabelField("null");
                }
                else
                {
                    DrawObjectFieldsShallow(elem);
                }
                EditorGUI.indentLevel--;
            }

            return elem;
        }

        private void DrawObjectFieldsShallow(object obj)
        {
            Type t = obj.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (var f in t.GetFields(flags))
            {
                if (f.IsDefined(typeof(HideInInspector), false)) continue;
                string name = PrettyName(f.Name);
                object v = f.GetValue(obj);
                Type ft = f.FieldType;

                // 仅绘制基础类型/Unity 对象，避免容器内置变量
                if (typeof(UnityEngine.Object).IsAssignableFrom(ft))
                {
                    var newObj = EditorGUILayout.ObjectField(name, (UnityEngine.Object)v, ft, true);
                    if (!Equals(newObj, v)) f.SetValue(obj, newObj);
                }
                else if (ft == typeof(int))
                {
                    int nv = EditorGUILayout.IntField(name, v != null ? (int)v : 0);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(long))
                {
                    long nv = EditorGUILayout.LongField(name, v != null ? (long)v : 0L);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(float))
                {
                    float nv = EditorGUILayout.FloatField(name, v != null ? (float)v : 0f);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(double))
                {
                    double nv = EditorGUILayout.DoubleField(name, v != null ? (double)v : 0d);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(bool))
                {
                    bool nv = EditorGUILayout.Toggle(name, v != null && (bool)v);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(string))
                {
                    string nv = EditorGUILayout.TextField(name, v as string ?? string.Empty);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (ft.IsEnum)
                {
                    Enum ev = v as Enum ?? (Enum)Activator.CreateInstance(ft);
                    Enum nv = (Enum)EditorGUILayout.EnumPopup(name, ev);
                    if (!Equals(nv, v)) f.SetValue(obj, nv);
                }
                else if (typeof(Entity).IsAssignableFrom(ft))
                {
                    var ent = v as Entity;
                    GameObject go = ent != null ? ent.ViewGO : null;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(name, go, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    // 其他复杂类型只展示名称，避免深入容器内部字段
                    EditorGUILayout.LabelField($"{name}: {(v == null ? "null" : ft.Name)}");
                }
            }
        }

        private static string PrettyName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            if (raw.Length > 17 && raw.Contains("k__BackingField"))
                return raw.Substring(1, raw.Length - 17);
            return raw;
        }

        private static string GetFoldKey(string label, object instance)
        {
            int id = instance != null ? instance.GetHashCode() : 0;
            return label + "#" + id;
        }

        private static bool GetFoldout(string key)
        {
            return s_Foldouts.TryGetValue(key, out bool v) && v;
        }
        private static void SetFoldout(string key, bool v)
        {
            s_Foldouts[key] = v;
        }
    }
}

