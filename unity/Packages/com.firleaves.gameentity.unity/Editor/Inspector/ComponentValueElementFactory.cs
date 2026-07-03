using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameEntity.Unity.Editor
{
    internal static class ComponentValueElementFactory
    {
        public static VisualElement CreateMemberElement(ComponentMemberDescriptor member, ComponentValueElementContext context)
        {
            if (member.ReadException != null)
            {
                VisualElement row = ComponentViewInspectorStyles.CreateRow(member.DisplayName);
                row.Add(ComponentViewInspectorStyles.CreateWarning($"{member.RawName}: {member.ReadException.Message}"));
                return row;
            }

            return CreateValueElement(member.DisplayName, member.MemberType, member.Value, member.Path, 0, context);
        }

        public static VisualElement CreateValueElement(
            string label,
            Type declaredType,
            object value,
            string path,
            int depth,
            ComponentValueElementContext context)
        {
            Type effectiveType = Nullable.GetUnderlyingType(declaredType) ?? declaredType ?? value?.GetType() ?? typeof(object);

            if (value == null)
            {
                return CreateReadOnlyText(label, "null");
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(effectiveType) ||
                typeof(Entity).IsAssignableFrom(effectiveType) ||
                ComponentValueElementUtility.IsEntityRef(effectiveType))
            {
                return ObjectReferenceElementFactory.CreateObjectField(label, effectiveType, value);
            }

            if (IsPrimitiveLike(effectiveType))
            {
                return CreatePrimitive(label, effectiveType, value);
            }

            if (IsUnityStruct(effectiveType))
            {
                return CreateUnityStruct(label, effectiveType, value);
            }

            if (ComponentValueElementUtility.IsDictionaryLike(effectiveType))
            {
                return CreateDictionary(label, value, path, depth, context);
            }

            if (effectiveType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(effectiveType))
            {
                return CreateEnumerable(label, value, path, depth, context);
            }

            if (effectiveType.IsClass)
            {
                return CreateComplexObject(label, effectiveType, value, path, depth, context);
            }

            return CreateReadOnlyText(label, ComponentValueElementUtility.FormatValue(value));
        }

        private static VisualElement CreatePrimitive(string label, Type type, object value)
        {
            if (type == typeof(bool))
            {
                VisualElement row = ComponentViewInspectorStyles.CreateRow(label);
                Toggle toggle = new Toggle { value = (bool)value };
                toggle.SetEnabled(false);
                toggle.style.flexGrow = 1;
                row.Add(toggle);
                return row;
            }

            if (type.IsEnum)
            {
                VisualElement row = ComponentViewInspectorStyles.CreateRow(label);
                EnumField field = new EnumField((Enum)value);
                field.SetEnabled(false);
                field.style.flexGrow = 1;
                row.Add(field);
                return row;
            }

            string text = value is DateTime dateTime ? dateTime.ToString("O") : ComponentValueElementUtility.FormatValue(value);
            return CreateReadOnlyText(label, text);
        }

        private static VisualElement CreateUnityStruct(string label, Type type, object value)
        {
            VisualElement row = ComponentViewInspectorStyles.CreateRow(label);
            VisualElement field;

            if (type == typeof(Vector2))
            {
                field = new Vector2Field { value = (Vector2)value };
            }
            else if (type == typeof(Vector3))
            {
                field = new Vector3Field { value = (Vector3)value };
            }
            else if (type == typeof(Vector4))
            {
                field = new Vector4Field { value = (Vector4)value };
            }
            else if (type == typeof(Color))
            {
                field = new ColorField { value = (Color)value };
            }
            else if (type == typeof(Rect))
            {
                field = new RectField { value = (Rect)value };
            }
            else if (type == typeof(Bounds))
            {
                field = new BoundsField { value = (Bounds)value };
            }
            else if (type == typeof(AnimationCurve))
            {
                field = new CurveField { value = (AnimationCurve)value };
            }
            else
            {
                return CreateReadOnlyText(label, ComponentValueElementUtility.FormatValue(value));
            }

            field.SetEnabled(false);
            field.style.flexGrow = 1;
            row.Add(field);
            return row;
        }

        private static VisualElement CreateDictionary(
            string label,
            object value,
            string path,
            int depth,
            ComponentValueElementContext context)
        {
            int count = ComponentValueElementUtility.GetCollectionCount(value);
            Foldout foldout = CreateFoldout($"{label} [{count}]", path, context);
            foldout.AddToClassList("ge-dictionary");

            BuildWhenExpanded(foldout, () =>
            {
                int index = 0;
                foreach (KeyValuePair<object, object> entry in ComponentValueElementUtility.EnumerateDictionary(value))
                {
                    if (index >= context.MaxCollectionItems)
                    {
                        foldout.Add(ComponentViewInspectorStyles.CreateMutedText($"Showing first {context.MaxCollectionItems} items."));
                        break;
                    }

                    object itemValue = entry.Value;
                    Type itemType = itemValue?.GetType() ?? typeof(object);
                    string key = ComponentValueElementUtility.FormatValue(entry.Key);
                    VisualElement item = CreateValueElement($"[{key}]", itemType, itemValue, $"{path}.{index}", depth + 1, context);
                    item.style.marginLeft = 12;
                    foldout.Add(item);
                    index++;
                }
            });

            return foldout;
        }

        private static VisualElement CreateEnumerable(
            string label,
            object value,
            string path,
            int depth,
            ComponentValueElementContext context)
        {
            int count = ComponentValueElementUtility.GetCollectionCount(value);
            Foldout foldout = CreateFoldout($"{label} [{count}]", path, context);
            foldout.AddToClassList("ge-enumerable");

            BuildWhenExpanded(foldout, () =>
            {
                int index = 0;
                foreach (object itemValue in (IEnumerable)value)
                {
                    if (index >= context.MaxCollectionItems)
                    {
                        foldout.Add(ComponentViewInspectorStyles.CreateMutedText($"Showing first {context.MaxCollectionItems} items."));
                        break;
                    }

                    Type itemType = itemValue?.GetType() ?? typeof(object);
                    VisualElement item = CreateValueElement($"[{index}]", itemType, itemValue, $"{path}.{index}", depth + 1, context);
                    item.style.marginLeft = 12;
                    foldout.Add(item);
                    index++;
                }
            });

            return foldout;
        }

        private static VisualElement CreateComplexObject(
            string label,
            Type type,
            object value,
            string path,
            int depth,
            ComponentValueElementContext context)
        {
            if (depth >= context.MaxDepth)
            {
                return CreateReadOnlyText(label, $"{type.Name} (max depth)");
            }

            Foldout foldout = CreateFoldout($"{label} ({type.Name})", path, context);
            BuildWhenExpanded(foldout, () =>
            {
                if (!context.TryEnterObject(value))
                {
                    foldout.Add(ComponentViewInspectorStyles.CreateMutedText($"{type.Name} (cycle)"));
                    return;
                }

                try
                {
                    IReadOnlyList<ComponentMemberDescriptor> members = ComponentInspectorReflection.GetInspectableMembers(
                        value,
                        context.ShowPrivate,
                        context.ShowProperties,
                        context.ShowRawFrameworkFields);

                    int visibleCount = 0;
                    foreach (ComponentMemberDescriptor member in members)
                    {
                        if (!ShouldShowMember(member, context))
                        {
                            continue;
                        }

                        VisualElement item = CreateMemberElement(member, context);
                        item.style.marginLeft = 12;
                        foldout.Add(item);
                        visibleCount++;
                    }

                    if (visibleCount == 0)
                    {
                        foldout.Add(ComponentViewInspectorStyles.CreateMutedText("No members."));
                    }
                }
                finally
                {
                    context.ExitObject(value);
                }
            });

            return foldout;
        }

        public static bool ShouldShowMember(ComponentMemberDescriptor member, ComponentValueElementContext context)
        {
            if (!context.ShowNullValues && member.ReadException == null && member.Value == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(context.SearchText))
            {
                return true;
            }

            if (member.ReadException != null)
            {
                return ComponentInspectorReflection.MatchesSearch(
                    context.SearchText,
                    member.DisplayName,
                    member.RawName,
                    member.MemberType,
                    member.ReadException.Message);
            }

            return ComponentInspectorReflection.MatchesSearch(
                context.SearchText,
                member.DisplayName,
                member.RawName,
                member.MemberType,
                member.Value);
        }

        private static VisualElement CreateReadOnlyText(string label, string value)
        {
            VisualElement row = ComponentViewInspectorStyles.CreateRow(label);
            TextField field = new TextField { value = value ?? string.Empty };
            field.SetEnabled(false);
            field.style.flexGrow = 1;
            row.Add(field);
            return row;
        }

        private static Foldout CreateFoldout(string title, string path, ComponentValueElementContext context)
        {
            Foldout foldout = new Foldout
            {
                text = title,
                value = context.GetFoldoutState(path, false)
            };
            foldout.RegisterValueChangedCallback(evt => context.SetFoldoutState(path, evt.newValue));
            return foldout;
        }

        private static void BuildWhenExpanded(Foldout foldout, Action build)
        {
            bool built = false;
            Action buildOnce = () =>
            {
                if (built)
                {
                    return;
                }

                built = true;
                build();
            };

            if (foldout.value)
            {
                buildOnce();
            }

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    buildOnce();
                }
            });
        }

        private static bool IsPrimitiveLike(Type type)
        {
            return type == typeof(bool) ||
                   type == typeof(byte) ||
                   type == typeof(sbyte) ||
                   type == typeof(short) ||
                   type == typeof(ushort) ||
                   type == typeof(int) ||
                   type == typeof(uint) ||
                   type == typeof(long) ||
                   type == typeof(ulong) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal) ||
                   type == typeof(string) ||
                   type == typeof(char) ||
                   type == typeof(DateTime) ||
                   type.IsEnum;
        }

        private static bool IsUnityStruct(Type type)
        {
            return type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Color) ||
                   type == typeof(Rect) ||
                   type == typeof(Bounds) ||
                   type == typeof(AnimationCurve);
        }
    }
}
