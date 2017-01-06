﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using SqlBulkTools.Enumeration;

// ReSharper disable once CheckNamespace
namespace SqlBulkTools
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class AbstractOperation<T>
    {
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        // ReSharper disable InconsistentNaming
        protected ColumnDirectionType _outputIdentity;
        protected string _identityColumn;
        protected Dictionary<int, T> _outputIdentityDic;
        protected bool _disableAllIndexes;
        protected int _sqlTimeout;
        protected HashSet<string> _columns;
        protected int? _bulkCopyBatchSize;
        protected int? _bulkCopyNotifyAfter;
        protected HashSet<string> _disableIndexList;
        protected bool _bulkCopyEnableStreaming;
        protected int _bulkCopyTimeout;
        protected string _schema;
        protected string _tableName;
        protected Dictionary<string, string> _customColumnMappings;
        protected IEnumerable<T> _list;
        protected List<string> _matchTargetOn;
        protected SqlBulkCopyOptions _sqlBulkCopyOptions;
        protected IEnumerable<SqlRowsCopiedEventHandler> _bulkCopyDelegates;
        protected List<PredicateCondition> _updatePredicates;
        protected List<PredicateCondition> _deletePredicates;
        protected List<SqlParameter> _parameters;
        protected Dictionary<string, string> _collationColumnDic;
        protected int _conditionSortOrder;
        #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="tableName"></param>
        /// <param name="schema"></param>
        /// <param name="columns"></param>
        /// <param name="disableIndexList"></param>
        /// <param name="disableAllIndexes"></param>
        /// <param name="customColumnMappings"></param>
        /// <param name="sqlTimeout"></param>
        /// <param name="bulkCopyTimeout"></param>
        /// <param name="bulkCopyEnableStreaming"></param>
        /// <param name="bulkCopyNotifyAfter"></param>
        /// <param name="bulkCopyBatchSize"></param>
        /// <param name="sqlBulkCopyOptions"></param>
        /// <param name="bulkCopyDelegates"></param>
        protected AbstractOperation(IEnumerable<T> list, string tableName, string schema, HashSet<string> columns,
            HashSet<string> disableIndexList, bool disableAllIndexes,
            Dictionary<string, string> customColumnMappings, int sqlTimeout, int bulkCopyTimeout,
            bool bulkCopyEnableStreaming,
            int? bulkCopyNotifyAfter, int? bulkCopyBatchSize, SqlBulkCopyOptions sqlBulkCopyOptions, 
            IEnumerable<SqlRowsCopiedEventHandler> bulkCopyDelegates)
        {
            _list = list;
            _tableName = tableName;
            _schema = schema;
            _columns = columns;
            _disableIndexList = disableIndexList;
            _disableAllIndexes = disableAllIndexes;
            _customColumnMappings = customColumnMappings;
            _sqlTimeout = sqlTimeout;
            _bulkCopyTimeout = bulkCopyTimeout;
            _bulkCopyEnableStreaming = bulkCopyEnableStreaming;
            _bulkCopyNotifyAfter = bulkCopyNotifyAfter;
            _bulkCopyBatchSize = bulkCopyBatchSize;
            _sqlBulkCopyOptions = sqlBulkCopyOptions;
            _identityColumn = null;
            _collationColumnDic = new Dictionary<string, string>();
            _outputIdentityDic = new Dictionary<int, T>();
            _outputIdentity = ColumnDirectionType.Input;
            _matchTargetOn = new List<string>();
            _bulkCopyDelegates = bulkCopyDelegates;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <exception cref="SqlBulkToolsException"></exception>

        protected void SetIdentity(Expression<Func<T, object>> columnName)
        {
            var propertyName = BulkOperationsHelper.GetPropertyName(columnName);

            if (propertyName == null)
                throw new SqlBulkToolsException("SetIdentityColumn column name can't be null");

            if (_identityColumn == null)
                _identityColumn = propertyName;

            else
            {
                throw new SqlBulkToolsException("Can't have more than one identity column");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <exception cref="SqlBulkToolsException"></exception>
        protected void SetIdentity(string columnName)
        {

            if (columnName == null)
                throw new SqlBulkToolsException("SetIdentityColumn column name can't be null");

            if (_identityColumn == null)
                _identityColumn = columnName;

            else
            {
                throw new SqlBulkToolsException("Can't have more than one identity column");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterToCheck"></param>
        /// <typeparam name="TParameter"></typeparam>
        /// <returns></returns>
        public static TParameter GetParameterValue<TParameter>(Expression<Func<TParameter>> parameterToCheck)
        {
            TParameter parameterValue = (TParameter)parameterToCheck.Compile().Invoke();

            return parameterValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="outputIdentity"></param>
        protected void SetIdentity(Expression<Func<T, object>> columnName, ColumnDirectionType outputIdentity)
        {
            _outputIdentity = outputIdentity;
            SetIdentity(columnName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="outputIdentity"></param>
        protected void SetIdentity(string columnName, ColumnDirectionType outputIdentity)
        {
            _outputIdentity = outputIdentity;
            SetIdentity(columnName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="collation"></param>
        /// <returns></returns>
        protected void SetCollation(Expression<Func<T, object>> columnName, string collation)
        {
            var propertyName = BulkOperationsHelper.GetPropertyName(columnName);

            if (propertyName == null)
                throw new SqlBulkToolsException("SetCollationOnColumn column name can't be null");

            _collationColumnDic.Add(propertyName, collation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="SqlBulkToolsException"></exception>
        protected void MatchTargetCheck()
        {
            if (_matchTargetOn.Count == 0)
            {
                throw new SqlBulkToolsException("MatchTargetOn list is empty when it's required for this operation. " +
                                                    "This is usually the primary key of your table but can also be more than one " +
                                                    "column depending on your business rules.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="SqlBulkToolsException"></exception>
        protected void IndexCheck()
        {
            if (_disableAllIndexes && (_disableIndexList != null && _disableIndexList.Any()))
            {
                throw new SqlBulkToolsException("Invalid setup. If \'TmpDisableAllNonClusteredIndexes\' is invoked, you can not use " +
                                                    "the \'AddTmpDisableNonClusteredIndex\' method.");
            }
        }
    }
}
