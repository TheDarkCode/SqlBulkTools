﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SqlBulkTools.Enumeration;

// ReSharper disable once CheckNamespace
namespace SqlBulkTools
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleDeleteQueryReady<T> : ITransaction
    {
        private readonly string _tableName;
        private readonly string _schema;
        private readonly int _sqlTimeout;
        private readonly List<PredicateCondition> _whereConditions;
        private readonly List<PredicateCondition> _andConditions;
        private readonly List<PredicateCondition> _orConditions;
        private readonly List<SqlParameter> _parameters;
        private int _conditionSortOrder;
        private readonly Dictionary<string, string> _collationColumnDic; 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schema"></param>
        /// <param name="sqlTimeout"></param>
        /// <param name="conditionSortOrder"></param>
        /// <param name="whereConditions"></param>
        /// <param name="parameters"></param>
        public SimpleDeleteQueryReady(string tableName, string schema,
            int sqlTimeout, int conditionSortOrder, List<PredicateCondition> whereConditions, List<SqlParameter> parameters)
        {
            _tableName = tableName;
            _schema = schema;
            _sqlTimeout = sqlTimeout;
            _whereConditions = whereConditions;
            _andConditions = new List<PredicateCondition>();
            _orConditions = new List<PredicateCondition>();
            _conditionSortOrder = conditionSortOrder;
            _parameters = parameters;
            _collationColumnDic = new Dictionary<string, string>();
        }


        /// <summary>
        /// Specify an additional condition to match on.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="SqlBulkToolsException"></exception>
        public SimpleDeleteQueryReady<T> And(Expression<Func<T, bool>> expression)
        {
            BulkOperationsHelper.AddPredicate(expression, PredicateType.And, _andConditions, _parameters,
                _conditionSortOrder, appendParam: Constants.UniqueParamIdentifier);
            _conditionSortOrder++;
            return this;
        }

        /// <summary>
        /// Specify an additional condition to match on.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="SqlBulkToolsException"></exception>
        public SimpleDeleteQueryReady<T> Or(Expression<Func<T, bool>> expression)
        {
            BulkOperationsHelper.AddPredicate(expression, PredicateType.Or, _orConditions, _parameters,
                _conditionSortOrder, appendParam: Constants.UniqueParamIdentifier);
            _conditionSortOrder++;
            return this;
        }

        /// <summary>
        /// Set the collation explicitly for join conditions. Can be recalled multiple times for more than one column. 
        /// Note that this should only be used if there is a collation conflict and you can't resolve it by other means. 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="collation"></param>
        /// <returns></returns>
        public SimpleDeleteQueryReady<T> SetCollationOnColumn(Expression<Func<T, object>> columnName, string collation)
        {
            var propertyName = BulkOperationsHelper.GetPropertyName(columnName);

            if (propertyName == null)
                throw new SqlBulkToolsException("SetCollationOnColumn column name can't be null");

            _collationColumnDic.Add(propertyName, collation);

            return this;
        }

        /// <summary>
        /// Commits a transaction to database. A valid setup must exist for the operation to be 
        /// successful.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public int Commit(SqlConnection connection)
        {
            var concatenatedQuery = _whereConditions.Concat(_andConditions).Concat(_orConditions).OrderBy(x => x.SortOrder);

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            SqlCommand command = connection.CreateCommand();
            command.Connection = connection;
            command.CommandTimeout = _sqlTimeout;

            string fullQualifiedTableName = BulkOperationsHelper.GetFullQualifyingTableName(connection.Database, _schema,
                _tableName);

            string comm = $"DELETE FROM {fullQualifiedTableName} " +
                          $"{BulkOperationsHelper.BuildPredicateQuery(concatenatedQuery, _collationColumnDic)}";

            command.CommandText = comm;

            if (_parameters.Count > 0)
            {
                command.Parameters.AddRange(_parameters.ToArray());
            }

            int affectedRows = command.ExecuteNonQuery();

            return affectedRows;
        }

        /// <summary>
        /// Commits a transaction to database asynchronously. A valid setup must exist for the operation to be 
        /// successful.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<int> CommitAsync(SqlConnection connection)
        {
            var concatenatedQuery = _whereConditions.Concat(_andConditions).Concat(_orConditions).OrderBy(x => x.SortOrder);

            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync();

            SqlCommand command = connection.CreateCommand();
            command.Connection = connection;
            command.CommandTimeout = _sqlTimeout;

            string fullQualifiedTableName = BulkOperationsHelper.GetFullQualifyingTableName(connection.Database, _schema,
                _tableName);

            string comm = $"DELETE FROM {fullQualifiedTableName} " +
                          $"{BulkOperationsHelper.BuildPredicateQuery(concatenatedQuery, _collationColumnDic)}";

            command.CommandText = comm;

            if (_parameters.Count > 0)
            {
                command.Parameters.AddRange(_parameters.ToArray());
            }

            int affectedRows = await command.ExecuteNonQueryAsync();

            return affectedRows;
        }
    }
}
