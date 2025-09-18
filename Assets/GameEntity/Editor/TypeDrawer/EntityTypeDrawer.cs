#if ENABLE_VIEW
using System;
using UnityEditor;
using UnityEngine;

namespace GE
{
    [TypeDrawer]
    public class EntityTypeDrawer : ITypeDrawer
    {
        public bool HandlesType(Type type)
        {
            return typeof(Entity).IsAssignableFrom(type);
        }

        public object DrawAndGetNewValue(Type memberType, string memberName, object value, object target)
        {
            var entity = value as Entity;
            GameObject go = entity != null ? entity.ViewGO : null;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(memberName, go, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();

            // 不修改实体引用，仅展示其 ViewGO
            return value;
        }
    }
}
#endif

