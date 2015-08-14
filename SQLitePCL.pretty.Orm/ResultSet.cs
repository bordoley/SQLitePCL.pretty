using System;
using System.Collections.Generic;
using SQLitePCL.pretty.Orm.Attributes;
using System.Reflection;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Extensions methods for SQLite ResultSet rows
    /// </summary>
    public static partial class ResultSet
    {

        /// <summary>
        /// Returns a row selector function that converts a SQLite row into an instance of the mutable type T.
        /// </summary>
        /// <returns>The result selector function.</returns>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Func<IReadOnlyList<IResultSetValue>,T> RowToObject<T>()
        {
            Func<T> builder = () => Activator.CreateInstance<T>();
            Func<T, T> build = obj => obj;

            return RowToObject(builder, build);
        }

        /// <summary>
        /// Returns a function that convert a SQLite row into an instance of type T using the provided builder functions. The builder 
        /// function may return the same instane of builder more than once, provided that the instance is thread local or locked such
        /// that only a single given thread ever has access to the instance between calls to builder and build.
        /// </summary>
        /// <returns>The result selector function.</returns>
        /// <param name="builder">A function that provides a builder object that can be used to build an instance of T.</param>
        /// <param name="build">A function that builds and instance of T using the builder.</param>
        /// <typeparam name="TBuilder">The builder type.</typeparam>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Func<IReadOnlyList<IResultSetValue>,T> RowToObject<TBuilder, T>(Func<TBuilder> builder, Func<TBuilder,T> build)
        {   
            Contract.Requires(builder != null);
            Contract.Requires(build != null);

            var columns = new Dictionary<string, PropertyInfo>();
            var tableName = TableMapping.Get<T>().TableName;

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
                        var columnInfo = column.ColumnInfo;
                        PropertyInfo prop;
                        if (columnInfo.TableName == tableName && columns.TryGetValue(columnInfo.OriginName, out prop))
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
                throw new NotSupportedException ($"Don't know how to read {clrType}");
            }
        }
    }
}

