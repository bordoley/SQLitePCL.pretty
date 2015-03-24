using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SQLitePCL.pretty.Orm
{
    public interface ISqlQuery
    {
        string ToSql();
    }
}

