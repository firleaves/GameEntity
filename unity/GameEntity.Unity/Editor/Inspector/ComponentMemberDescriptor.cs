using System;

namespace GameEntity.Unity.Editor
{
    internal enum ComponentMemberKind
    {
        Field,
        Property
    }

    internal sealed class ComponentMemberDescriptor
    {
        public ComponentMemberDescriptor(
            ComponentMemberKind kind,
            string displayName,
            string rawName,
            Type declaringType,
            Type memberType,
            object value,
            Exception readException,
            string path)
        {
            Kind = kind;
            DisplayName = displayName;
            RawName = rawName;
            DeclaringType = declaringType;
            MemberType = memberType;
            Value = value;
            ReadException = readException;
            Path = path;
        }

        public ComponentMemberKind Kind { get; }

        public string DisplayName { get; }

        public string RawName { get; }

        public Type DeclaringType { get; }

        public Type MemberType { get; }

        public object Value { get; }

        public Exception ReadException { get; }

        public string Path { get; }
    }
}
