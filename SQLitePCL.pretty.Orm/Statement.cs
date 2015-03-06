using System;

namespace SQLitePCL.pretty.Orm
{
    public static class Statement
    {
        public static void Bind<T>(this IStatement This, ITableMapping<T> tableMapping, T obj)
        {
            foreach (var column in tableMapping)
            {
                var key = ":" + column.Key;
                var value = column.Value.Property.GetValue(obj);
                This.BindParameters[key].Bind(value);
            }
        }

        public static void Execute<T>(this IStatement This, ITableMapping<T> tableMapping, T obj)
        {
            This.Reset();
            This.ClearBindings();
            This.Bind(tableMapping, obj);
            This.MoveNext();
        }
    }
}

