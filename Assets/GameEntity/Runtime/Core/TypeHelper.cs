using System;
using System.Collections.Generic;
using System.Reflection;

namespace GE
{
    public static class TypeHelper
    {
        // 缓存类型的特性信息
        private static readonly Dictionary<Type, Dictionary<Type, Attribute>> _typeAttributeDict = new Dictionary<Type, Dictionary<Type, Attribute>>();

        /// <summary>
        /// 检查类型是否有特定特性
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">类型</param>
        public static bool HasAttribute<T>(Type type) where T : Attribute
        {
            return GetAttribute<T>(type) != null;
        }

        /// <summary>
        /// 获取类型的特定特性
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">类型</param>
        public static T GetAttribute<T>(Type type) where T : Attribute
        {
            if (!_typeAttributeDict.TryGetValue(type, out Dictionary<Type, Attribute> attributes))
            {
                attributes = new Dictionary<Type, Attribute>();
                _typeAttributeDict.Add(type, attributes);

                foreach (Attribute attr in type.GetCustomAttributes(true))
                {
                    Type attrType = attr.GetType();
                    attributes[attrType] = attr;
                }
            }

            if (attributes.TryGetValue(typeof(T), out Attribute attribute))
            {
                return attribute as T;
            }

            return null;
        }

        /// <summary>
        /// 获取所有带有特定特性的类型
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        public static List<Type> GetTypesByAttribute<T>() where T : Attribute
        {
            List<Type> result = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (HasAttribute<T>(type))
                    {
                        result.Add(type);
                    }
                }
            }

            return result;
        }
    }
}
