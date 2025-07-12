using System;

namespace GE
{
    public interface ITypeDrawer
    {
        bool HandlesType(Type type);

        object DrawAndGetNewValue(Type memberType, string memberName, object value, object target);
    }
}