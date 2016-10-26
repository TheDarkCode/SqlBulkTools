﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SqlBulkTools
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UpdateCollectionQuery<T>
    {
        private readonly DataTable _smallCollection; 
        private readonly string _tableName;
        private readonly string _schema;
        private readonly HashSet<string> _columns;
        private readonly Dictionary<string, string> _customColumnMappings;
        private readonly int _sqlTimeout;
        private readonly BulkOperations _ext;
        private readonly List<Condition> _whereConditions;
        private readonly List<Condition> _andConditions;
        private readonly List<Condition> _orConditions;
        private int _conditionSortOrder;
        private readonly List<SqlParameter> _sqlParams;
        private int _transactionCount;
        private string _databaseIdentifier;
        private List<string> _concatTrans;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleEntity"></param>
        /// <param name="tableName"></param>
        /// <param name="schema"></param>
        /// <param name="columns"></param>
        /// <param name="customColumnMappings"></param>
        /// <param name="sqlTimeout"></param>
        /// <param name="ext"></param>
        public UpdateCollectionQuery(DataTable smallCollection, string tableName, string schema, HashSet<string> columns, 
            Dictionary<string, string> customColumnMappings, int sqlTimeout, BulkOperations ext, int transactionCount, string databaseIdentifier, List<string> concatTrans, List<SqlParameter> sqlParams)
        {
            _smallCollection = smallCollection;
            _tableName = tableName;
            _schema = schema;
            _columns = columns;
            _customColumnMappings = customColumnMappings;
            _sqlTimeout = sqlTimeout;
            _ext = ext;
            _whereConditions = new List<Condition>();
            _andConditions = new List<Condition>();
            _orConditions = new List<Condition>();
            _sqlParams = sqlParams;
            _conditionSortOrder = 1;
            _transactionCount = transactionCount;
            _databaseIdentifier = databaseIdentifier;
            _concatTrans = concatTrans;
        }

        /// <summary>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public UpdateCollectionQueryReady<T> Where(Expression<Func<T, bool>> expression)
        {
            // _whereConditions list will only ever contain one element.
            BulkOperationsHelper.AddPredicate(expression, PredicateType.Where, _whereConditions, _sqlParams, 
                _conditionSortOrder, appendParam: Constants.UniqueParamIdentifier);

            _conditionSortOrder++;

            return new UpdateCollectionQueryReady<T>(_smallCollection, _tableName, _schema, _columns, _customColumnMappings, 
                _sqlTimeout, _ext, _conditionSortOrder, _whereConditions, _sqlParams, _transactionCount, _databaseIdentifier, _concatTrans);
        }

    }
}