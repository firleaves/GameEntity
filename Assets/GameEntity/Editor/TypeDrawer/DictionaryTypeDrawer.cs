using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GE
{
    [TypeDrawer]
    public class DictionaryTypeDrawer : ITypeDrawer
    {
        private static readonly System.Collections.Generic.Dictionary<string, bool> s_Foldouts = new System.Collections.Generic.Dictionary<string, bool>();

        public bool HandlesType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.Dictionary<,>);
        }

        public object DrawAndGetNewValue(Type memberType, string memberName, object value, object target)
        {
            Type[] args = memberType.GetGenericArguments();
            Type keyType = args[0];
            Type valueType = args[1];

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

            string foldKey = GetFoldKey(memberName, target);
            bool isExpanded = GetFoldout(foldKey);
            int count = (int)memberType.GetProperty("Count").GetValue(value);
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{memberName} [{count}]", true);
            SetFoldout(foldKey, isExpanded);

            if (!isExpanded) return value;

            EditorGUI.indentLevel++;

            IEnumerable enumerable = (IEnumerable)value;
            int index = 0;
            foreach (object kv in enumerable)
            {
                if (kv == null)
                {
                    EditorGUILayout.LabelField($"[{index}]: null");
                    index++;
                    continue;
                }

                object k = kv.GetType().GetProperty("Key").GetValue(kv);
                object v = kv.GetType().GetProperty("Value").GetValue(kv);

                string keyLabel = KeyToString(k, keyType);

                // 值绘制（常用类型可编辑，复杂类型浅展示）
                object newV = DrawValue(valueType, keyLabel, v);
                if (!Equals(newV, v))
                {
                    // 更新字典 value：通过索引器 set_Item
                    var setItem = memberType.GetMethod("set_Item");
                    setItem.Invoke(value, new object[] { k, newV });
                }

                index++;
            }

            EditorGUI.indentLevel--;

            return value;
        }

        private string KeyToString(object key, Type keyType)
        {
            if (key == null) return "null";
            if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
            {
                var obj = key as UnityEngine.Object;
                return obj != null ? obj.name : "null";
            }
            return key.ToString();
        }

        private object DrawValue(Type type, string label, object v)
        {
            // Unity 对象
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return EditorGUILayout.ObjectField(label, (UnityEngine.Object)v, type, true);
            }

            // Entity：直接显示其 ViewGO
            if (typeof(Entity).IsAssignableFrom(type))
            {
                var ent = v as Entity;
                GameObject go = ent != null ? ent.ViewGO : null;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(label, go, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();
                return v;
            }

            // EntityRef<T>：显示其指向实体的 ViewGO
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntityRef<>))
            {
                if (v != null)
                {
                    var fi = type.GetField("entity", BindingFlags.NonPublic | BindingFlags.Instance);
                    var ent = fi != null ? fi.GetValue(v) as Entity : null;
                    GameObject go = ent != null ? ent.ViewGO : null;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(label, go, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.LabelField(label, "null");
                }
                return v;
            }

            // 基础/常用类型
            if (type == typeof(int))
            {
                int nv = EditorGUILayout.IntField(label, v != null ? (int)v : 0);
                return nv;
            }
            if (type == typeof(long))
            {
                long nv = EditorGUILayout.LongField(label, v != null ? (long)v : 0L);
                return nv;
            }
            if (type == typeof(float))
            {
                float nv = EditorGUILayout.FloatField(label, v != null ? (float)v : 0f);
                return nv;
            }
            if (type == typeof(double))
            {
                double nv = EditorGUILayout.DoubleField(label, v != null ? (double)v : 0d);
                return nv;
            }
            if (type == typeof(bool))
            {
                bool nv = EditorGUILayout.Toggle(label, v != null && (bool)v);
                return nv;
            }
            if (type == typeof(string))
            {
                string nv = EditorGUILayout.TextField(label, v as string ?? string.Empty);
                return nv;
            }
            if (type.IsEnum)
            {
                Enum ev = v as Enum ?? (Enum)Activator.CreateInstance(type);
                Enum nv = (Enum)EditorGUILayout.EnumPopup(label, ev);
                return nv;
            }

            // 复杂类型：折叠展示浅层字段
            string foldKey = GetFoldKey(label, v);
            bool expanded = GetFoldout(foldKey);
            expanded = EditorGUILayout.Foldout(expanded, $"{label}: {type.Name}", true);
            SetFoldout(foldKey, expanded);
            if (expanded)
            {
                EditorGUI.indentLevel++;
                if (v == null)
                {
                    EditorGUILayout.LabelField("null");
                }
                else
                {
                    DrawObjectFieldsShallow(v);
                }
                EditorGUI.indentLevel--;
            }

            return v;
        }

        private void DrawObjectFieldsShallow(object obj)
        {
            Type t = obj.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (var f in t.GetFields(flags))
            {
                if (f.IsDefined(typeof(HideInInspector), false)) continue;
                string name = PrettyName(f.Name);
                object val = f.GetValue(obj);
                Type ft = f.FieldType;

                if (typeof(UnityEngine.Object).IsAssignableFrom(ft))
                {
                    var newObj = EditorGUILayout.ObjectField(name, (UnityEngine.Object)val, ft, true);
                    if (!Equals(newObj, val)) f.SetValue(obj, newObj);
                }
                else if (ft == typeof(int))
                {
                    int nv = EditorGUILayout.IntField(name, val != null ? (int)val : 0);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(long))
                {
                    long nv = EditorGUILayout.LongField(name, val != null ? (long)val : 0L);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(float))
                {
                    float nv = EditorGUILayout.FloatField(name, val != null ? (float)val : 0f);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(double))
                {
                    double nv = EditorGUILayout.DoubleField(name, val != null ? (double)val : 0d);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(bool))
                {
                    bool nv = EditorGUILayout.Toggle(name, val != null && (bool)val);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else if (ft == typeof(string))
                {
                    string nv = EditorGUILayout.TextField(name, val as string ?? string.Empty);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else if (ft.IsEnum)
                {
                    Enum ev = val as Enum ?? (Enum)Activator.CreateInstance(ft);
                    Enum nv = (Enum)EditorGUILayout.EnumPopup(name, ev);
                    if (!Equals(nv, val)) f.SetValue(obj, nv);
                }
                else
                {
                    EditorGUILayout.LabelField($"{name}: {(val == null ? "null" : ft.Name)}");
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

