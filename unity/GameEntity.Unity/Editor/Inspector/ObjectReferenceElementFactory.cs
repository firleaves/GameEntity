using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameEntity.Unity.Editor
{
    internal enum ObjectReferenceKind
    {
        None,
        SceneObject,
        Asset,
        Missing
    }

    internal readonly struct ObjectReferenceTarget
    {
        public ObjectReferenceTarget(UnityEngine.Object unityObject, ObjectReferenceKind kind, Type objectType)
        {
            Object = unityObject;
            Kind = kind;
            ObjectType = objectType ?? typeof(UnityEngine.Object);
        }

        public UnityEngine.Object Object { get; }

        public ObjectReferenceKind Kind { get; }

        public Type ObjectType { get; }
    }

    internal static class ObjectReferenceElementFactory
    {
        public static VisualElement CreateObjectField(string label, Type declaredType, object value)
        {
            ObjectReferenceTarget target = Resolve(declaredType, value);
            VisualElement row = ComponentViewInspectorStyles.CreateRow(label);

            ObjectField objectField = new ObjectField
            {
                objectType = target.ObjectType,
                allowSceneObjects = true,
                value = target.Object
            };
            objectField.SetEnabled(false);
            objectField.style.flexGrow = 1;
            row.Add(objectField);

            if (target.Kind == ObjectReferenceKind.Missing)
            {
                row.Add(ComponentViewInspectorStyles.CreateWarning("missing"));
            }

            return row;
        }

        public static ObjectReferenceTarget Resolve(Type declaredType, object value)
        {
            Type effectiveType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (typeof(Entity).IsAssignableFrom(effectiveType) || ComponentValueElementUtility.IsEntityRef(effectiveType))
            {
                Entity entity = value as Entity ?? ComponentValueElementUtility.ReadEntityRef(value, effectiveType);
                GameObject gameObject = ResolveEntityGameObject(entity);
                return new ObjectReferenceTarget(
                    gameObject,
                    gameObject == null ? (entity == null ? ObjectReferenceKind.None : ObjectReferenceKind.Missing) : ObjectReferenceKind.SceneObject,
                    typeof(GameObject));
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(effectiveType))
            {
                UnityEngine.Object unityObject = value as UnityEngine.Object;
                ObjectReferenceKind kind = unityObject == null
                    ? ObjectReferenceKind.None
                    : AssetDatabase.Contains(unityObject)
                        ? ObjectReferenceKind.Asset
                        : ObjectReferenceKind.SceneObject;
                return new ObjectReferenceTarget(unityObject, kind, effectiveType);
            }

            return new ObjectReferenceTarget(null, ObjectReferenceKind.None, typeof(UnityEngine.Object));
        }

        public static GameObject ResolveEntityGameObject(Entity entity)
        {
            if (entity == null || UnityEntityViewRegistry.Active == null)
            {
                return null;
            }

            ComponentView view = UnityEntityViewRegistry.Active.GetView(entity);
            return view == null ? null : view.gameObject;
        }
    }
}
