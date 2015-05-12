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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm.Attributes;
using SQLitePCL.pretty.Orm.Sql;
using System.IO;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// DSL for generating SQLite queries using LINQ like method chaining.
    /// </summary>
    public static partial class SqlQuery
    {
        /// <summary>
        /// Query data from a single table.
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        public static FromClause<T> From<T>() =>
            new FromClause<T>(SqlCompiler.CompileFromClause(typeof(T)));

        /// <summary>
        /// Query data from two tables.
        /// </summary>
        /// <typeparam name="T1">The type of the first table.</typeparam>
        /// <typeparam name="T2">The type of the second table.</typeparam>
        public static FromClause<T1,T2> From<T1,T2>() =>
            new FromClause<T1,T2>(SqlCompiler.CompileFromClause(typeof(T1), typeof(T2)));

        /// <summary>
        /// Query data from two tables.
        /// </summary>
        /// <typeparam name="T1">The type of the first table.</typeparam>
        /// <typeparam name="T2">The type of the second table.</typeparam>
        /// <typeparam name="T3">The type of the third table.</typeparam>
        public static FromClause<T1,T2,T3> From<T1,T2,T3>() =>
            new FromClause<T1,T2,T3>(SqlCompiler.CompileFromClause(typeof(T1), typeof(T2), typeof(T3)));

        /// <summary>
        /// Query data from two tables.
        /// </summary>
        /// <typeparam name="T1">The type of the first table.</typeparam>
        /// <typeparam name="T2">The type of the second table.</typeparam>
        /// <typeparam name="T3">The type of the third table.</typeparam>
        /// <typeparam name="T4">The type of the fourth table.</typeparam>
        public static FromClause<T1,T2,T3,T4> From<T1,T2,T3,T4>() =>
            new FromClause<T1,T2,T3,T4>(SqlCompiler.CompileFromClause(typeof(T1), typeof(T2), typeof(T3), typeof(T4)));
    }
}
