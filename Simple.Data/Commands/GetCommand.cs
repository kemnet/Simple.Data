﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Simple.Data.Commands
{
    using Extensions;

    public class GetCommand : ICommand
    {
        public bool IsCommandFor(string method)
        {
            return method.Equals("get", StringComparison.OrdinalIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            var result = dataStrategy.Get(table.GetName(), args);
            return result == null ? null : new SimpleRecord(result, table.GetQualifiedName(), dataStrategy);
        }

        public object Execute(DataStrategy dataStrategy, SimpleQuery query, InvokeMemberBinder binder, object[] args)
        {
            var keyNames = dataStrategy.GetAdapter().GetKeyNames(query.TableName);
            var dict = keyNames.Select((k, i) => new KeyValuePair<string, object>(k, args[i]));
            query = query.Where(ExpressionHelper.CriteriaDictionaryToExpression(query.TableName, dict)).Take(1);
            return query.FirstOrDefault();
        }

        public Func<object[], object> CreateDelegate(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            if (dataStrategy is SimpleTransaction) return null;

            var func = dataStrategy.GetAdapter().OptimizingDelegateFactory.CreateGetDelegate(dataStrategy.GetAdapter(),
                                                                                         table.GetName(), args);
                return a =>
                           {
                               var data = func(a);
                               return (data != null && data.Count > 0)
                                          ? new SimpleRecord(data, table.GetQualifiedName(), dataStrategy)
                                          : null;
                           };
        }
    }
}
