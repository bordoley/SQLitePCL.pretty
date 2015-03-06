using System;

namespace SQLitePCL.pretty.Orm
{    
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class TableAttribute : Attribute
    {
        private readonly string _name;

        public TableAttribute (string name)
        {
            _name = name;
        }

        public string Name { get { return _name; } }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class ColumnAttribute : Attribute
    {
        private readonly string _name;

        public ColumnAttribute (string name)
        {
            _name = name;
        }

        public string Name { get { return _name; } }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class AutoIncrementAttribute : Attribute
    {
    }

    [AttributeUsage (AttributeTargets.Property)]
    public class IndexedAttribute : Attribute
    {
        private readonly string _name;
        private readonly int _order;
        private readonly bool _unique;
        
        public IndexedAttribute()
        {
        }

        public IndexedAttribute(string name, int order) : this(name, order, false)
        {}
        
        public IndexedAttribute(string name, int order, bool unique)
        {
            _name = name;
            _order = order;
            _unique = unique;
        }

        public string Name { get { return _name; } }
        public int Order { get { return _order; } }
        public bool Unique { get { return _unique; } }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class IgnoreAttribute : Attribute
    {
    }

    [AttributeUsage (AttributeTargets.Property)]
    public class UniqueAttribute : IndexedAttribute
    {
        // FIXME:
        public UniqueAttribute()
        {
        }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class MaxLengthAttribute : Attribute
    {
        private readonly int _value;

        public MaxLengthAttribute (int length)
        {
            _value = length;
        }

        public int Value { get { return _value; } }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class CollationAttribute: Attribute
    {
        private readonly string _value;

        public CollationAttribute (string collation)
        {
            _value = collation;
        }

        public string Value { get { return _value; } }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}