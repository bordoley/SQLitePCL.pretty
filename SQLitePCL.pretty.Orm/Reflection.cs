using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SQLitePCL.pretty.Orm
{
    internal static class Reflection
    {
        // http://stackoverflow.com/questions/2742276/in-c-how-do-i-check-if-a-type-is-a-subtype-or-the-type-of-an-object
        internal static bool IsSameOrSubclass(this Type This, Type typ)
        {
            return This.GetTypeInfo().IsSubclassOf(typ) || This == typ;
        }

        internal static bool IsNullable(this Type This)
        {   
            var typeInfo = This.GetTypeInfo();
            return (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        // If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
        internal static Type GetUnderlyingType(this Type This)
        {
            return Nullable.GetUnderlyingType(This) ?? This;
        }

        internal static object ConvertTo (this object This, Type t)
        {
            if (This == null) { return null; }
            var nut = t.GetUnderlyingType();   
            return Convert.ChangeType (This, nut);
        }

        internal static IEnumerable<PropertyInfo> GetPublicInstanceProperties(this Type This)
        {
            return This.GetRuntimeProperties().Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic);
        }

    }
}

