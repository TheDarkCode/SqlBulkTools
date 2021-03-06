﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq.Expressions;
using SqlBulkTools.Enumeration;

// ReSharper disable once CheckNamespace
namespace SqlBulkTools
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleUpdateQueryCondition<T>
    {
        private readonly T _singleEntity;
        private readonly string _tableName;
        private readonly string _schema;
        private readonly HashSet<string> _columns;
        private readonly Dictionary<string, string> _customColumnMappings;
        private readonly int _sqlTimeout;
        private readonly List<PredicateCondition> _whereConditions;
        private int _conditionSortOrder;
        private readonly List<SqlParameter> _sqlParams;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleEntity"></param>
        /// <param name="tableName"></param>
        /// <param name="schema"></param>
        /// <param name="columns"></param>
        /// <param name="customColumnMappings"></param>
        /// <param name="sqlTimeout"></param>
        /// <param name="sqlParams"></param>
        public SimpleUpdateQueryCondition(T singleEntity, string tableName, string schema, HashSet<string> columns, 
            Dictionary<string, string> customColumnMappings, int sqlTimeout, List<SqlParameter> sqlParams)
        {
            _singleEntity = singleEntity;
            _tableName = tableName;
            _schema = schema;
            _columns = columns;
            _customColumnMappings = customColumnMappings;
            _sqlTimeout = sqlTimeout;
            _whereConditions = new List<PredicateCondition>();
            _sqlParams = sqlParams;
            _conditionSortOrder = 1;
        }

        /// <summary>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public SimpleUpdateQueryReady<T> Where(Expression<Func<T, bool>> expression)
        {
            // _whereConditions list will only ever contain one element.
            BulkOperationsHelper.AddPredicate(expression, PredicateType.Where, _whereConditions, _sqlParams, 
                _conditionSortOrder, appendParam: Constants.UniqueParamIdentifier);

            _conditionSortOrder++;

            return new SimpleUpdateQueryReady<T>(_singleEntity, _tableName, _schema, _columns, _customColumnMappings, 
                _sqlTimeout, _conditionSortOrder, _whereConditions, _sqlParams);
        }

    }
}
