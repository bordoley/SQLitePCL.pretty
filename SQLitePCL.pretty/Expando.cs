using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SQLitePCL.pretty
{
    internal sealed class Expando<T>
        where T: class
    {
        public static Expando<T> Create()
        {
            return new Expando<T>();
        }

        private readonly ConditionalWeakTable<T, IDictionary<string, object>> table = 
            new ConditionalWeakTable<T, IDictionary<string, object>>();

        private Expando()
        {
        }

        private IDictionary<string, object> GetDictionary(T obj)
        {
            return table.GetValue(obj, _ => new Dictionary<string,object>());
        }

        public void SetValue(T obj, string key, object value)
        {
            GetDictionary(obj)[key] = value;
        }

        public TValue GetValue<TValue>(T obj, string key)
        {
            return (TValue) GetDictionary(obj)[key];
        }

        public bool TryGetValue<TValue>(T obj, string key, out TValue value)
        {
            object retval;
            if (GetDictionary(obj).TryGetValue(key, out retval))
            {
                value = (TValue) retval;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public TValue GetOrAddValue<TValue>(T obj, string key, Func<string, TValue> valueFactory)
        {
            object value;
            if (TryGetValue(obj, key, out value))
            {
                return (TValue) value;
            }

            value = valueFactory(key);
            GetDictionary(obj).Add(key, value);
            return (TValue) value;
        }

        public TValue GetOrAddValue<TValue>(T obj, string key, TValue value)
        {
            return GetOrAddValue(obj, key, _ => value);
        }
    }
}

