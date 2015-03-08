//
// Copyright (c) 2009-2015 Krueger Systems, Inc.
// Copyright (c) 2015 David Bordoley
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;

namespace SQLitePCL.pretty.Orm.Attributes
{    
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class TableAttribute : Attribute
    {
        private readonly string _name;
        private readonly CreateFlags _createFlags;

        public TableAttribute(CreateFlags createFlags) : this(null, CreateFlags.None)
        {
        }

        public TableAttribute(string name) : this(name, CreateFlags.None)
        {
        }

        public TableAttribute (string name, CreateFlags createFlags)
        {
            _name = name;
            _createFlags = createFlags;
        }

        public string Name { get { return _name; } }

        public CreateFlags CreateFlags { get { return _createFlags; } }
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


    // FIXME: This should be sealed maybe use an interface instead
    [AttributeUsage (AttributeTargets.Property)]
    public class IndexedAttribute : Attribute
    {
        private readonly string _name;
        private readonly int _order;
        private readonly bool _unique;
        
        public IndexedAttribute()
        {
        }

        public IndexedAttribute(string name, bool unique) : this(name, default(int), unique)
        {
        }

        public IndexedAttribute(string name, int order) : this(name, order, false) {}
        
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

    // FIXME: Not a fan of inheritance, maybe rework around an interface
    [AttributeUsage (AttributeTargets.Property)]
    public class UniqueAttribute : IndexedAttribute
    {
        public UniqueAttribute(string name, int order) : base(name, order, true)
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