using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using SQLitePCL.pretty.Orm.Attributes;
using System.Reflection;

namespace SQLitePCL.pretty.Orm
{
    public static partial class Orm
    {
        public static Func<IReadOnlyList<IResultSetValue>,T> ResultSetRowToObject<T>()
        {
            Func<T> builder = () => Activator.CreateInstance<T>();
            Func<T, T> build = obj => obj;

            return ResultSetRowToObject(builder, build);
        }

        public static Func<IReadOnlyList<IResultSetValue>,T> ResultSetRowToObject<TBuilder, T>(Func<TBuilder> builder, Func<TBuilder,T> build)
        {   
            Contract.Requires(builder != null);
            Contract.Requires(build != null);

            var columns = new Dictionary<string, PropertyInfo>();
            var typ = typeof(TBuilder);

            var props = typ.GetNotIgnoredSettableProperties();

            foreach (var prop in props)
            {
                var columnName = prop.GetColumnName();
                columns.Add(columnName, prop);
            }

            return (IReadOnlyList<IResultSetValue> row) =>
                {
                    var builderInst = builder();

                    foreach (var column in row)
                    {
                        PropertyInfo prop;
                        if (columns.TryGetValue(column.ColumnInfo.OriginName, out prop))
                        {
                            var value = column.ToObject(prop.PropertyType);
                            prop.SetValue(builderInst, value, null);
                        }
                    }

                    return build(builderInst);
                };
        }

        private static object ToObject(this ISQLiteValue value, Type clrType)
        {
            // Just in case the clrType is nullable
            clrType = clrType.GetUnderlyingType();

            if (value.SQLiteType == SQLiteType.Null)    { return null; } 
            else if (clrType == typeof(String))         { return value.ToString(); } 
            else if (clrType == typeof(Int32))          { return value.ToInt(); } 
            else if (clrType == typeof(Boolean))        { return value.ToBool(); } 
            else if (clrType == typeof(double))         { return value.ToDouble(); } 
            else if (clrType == typeof(float))          { return value.ToFloat(); } 
            else if (clrType == typeof(TimeSpan))       { return value.ToTimeSpan(); } 
            else if (clrType == typeof(DateTime))       { return value.ToDateTime(); } 
            else if (clrType == typeof(DateTimeOffset)) { return value.ToDateTimeOffset(); }
            else if (clrType.GetTypeInfo().IsEnum)      { return value.ToInt(); } 
            else if (clrType == typeof(Int64))          { return value.ToInt64(); } 
            else if (clrType == typeof(UInt32))         { return value.ToUInt32(); } 
            else if (clrType == typeof(decimal))        { return value.ToDecimal(); } 
            else if (clrType == typeof(Byte))           { return value.ToByte(); } 
            else if (clrType == typeof(UInt16))         { return value.ToUInt16(); } 
            else if (clrType == typeof(Int16))          { return value.ToShort(); } 
            else if (clrType == typeof(sbyte))          { return value.ToSByte(); } 
            else if (clrType == typeof(byte[]))         { return value.ToBlob(); } 
            else if (clrType == typeof(Guid))           { return value.ToGuid(); } 
            else if (clrType == typeof(Uri))            { return value.ToUri(); } 
            else 
            {
                throw new NotSupportedException ("Don't know how to read " + clrType);
            }
        }
    }
}

