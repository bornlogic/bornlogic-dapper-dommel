﻿using Dapper;
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dommel
{
    public static partial class DommelMapper
    {
        /// <summary>
        /// Determines whether there's any entity of type <typeparamref name="TEntity"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="connection">The connection to the database. This can either be open or closed.</param>
        /// <param name="transaction">Optional transaction for the command.</param>
        /// <returns><c>true</c> if there's at least one entity in the database; otherwise, <c>false</c>.</returns>
        public static bool Any<TEntity>(this IDbConnection connection, ITableNameResolver tableNameResolver, IDbTransaction? transaction = null)
        {
            var sql = BuildAnyAllSql(GetSqlBuilder(connection), typeof(TEntity), tableNameResolver);
            LogQuery<TEntity>(sql);
            return connection.ExecuteScalar<bool>(sql, transaction);
        }

        /// <summary>
        /// Determines whether there's any entity of type <typeparamref name="TEntity"/> in the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="connection">The connection to the database. This can either be open or closed.</param>
        /// <param name="transaction">Optional transaction for the command.</param>
        /// <returns><c>true</c> if there's at least one entity in the database; otherwise, <c>false</c>.</returns>
        public static Task<bool> AnyAsync<TEntity>(this IDbConnection connection, ITableNameResolver tableNameResolver, IDbTransaction? transaction = null)
        {
            var sql = BuildAnyAllSql(GetSqlBuilder(connection), typeof(TEntity), tableNameResolver);
            LogQuery<TEntity>(sql);
            return connection.ExecuteScalarAsync<bool>(sql, transaction);
        }

        /// <summary>
        /// Determines whether there's any entity of type <typeparamref name="TEntity"/> matching the specified predicate in the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="connection">The connection to the database. This can either be open or closed.</param>
        /// <param name="predicate">A predicate to filter the results.</param>
        /// <param name="transaction">Optional transaction for the command.</param>
        /// <returns><c>true</c> if there's at least one entity in the database that matches the specified predicate; otherwise, <c>false</c>.</returns>
        public static bool Any<TEntity>(this IDbConnection connection, Expression<Func<TEntity, bool>> predicate, ITableNameResolver tableNameResolver, IDbTransaction? transaction = null)
        {
            var sql = BuildAnySql(GetSqlBuilder(connection), predicate, tableNameResolver, out var parameters);
            LogQuery<TEntity>(sql);
            return connection.ExecuteScalar<bool>(sql, parameters, transaction);
        }

        /// <summary>
        /// Determines whether there's any entity of type <typeparamref name="TEntity"/> matching the specified predicate in the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="connection">The connection to the database. This can either be open or closed.</param>
        /// <param name="predicate">A predicate to filter the results.</param>
        /// <param name="transaction">Optional transaction for the command.</param>
        /// <returns><c>true</c> if there's at least one entity in the database that matches the specified predicate; otherwise, <c>false</c>.</returns>
        public static Task<bool> AnyAsync<TEntity>(this IDbConnection connection, Expression<Func<TEntity, bool>> predicate, ITableNameResolver tableNameResolver, IDbTransaction? transaction = null)
        {
            var sql = BuildAnySql(GetSqlBuilder(connection), predicate, tableNameResolver, out var parameters);
            LogQuery<TEntity>(sql);
            return connection.ExecuteScalarAsync<bool>(sql, parameters, transaction);
        }

        private static string BuildAnyPredicate(ISqlBuilder sqlBuilder, Type type, ITableNameResolver tableNameResolver)
        {
            var tableName = Resolvers.Table(type, sqlBuilder, tableNameResolver);
            var cacheKey = new QueryCacheKey(QueryCacheType.Any, sqlBuilder, type, tableName);
            if (!QueryCache.TryGetValue(cacheKey, out var sql))
            {
                sql = $"select 1 from {tableName}";
                QueryCache.TryAdd(cacheKey, sql);
            }

            return sql;
        }

        internal static string BuildAnyAllSql(ISqlBuilder sqlBuilder, Type type, ITableNameResolver tableNameResolver)
        {
            var sql = $"{BuildAnyPredicate(sqlBuilder, type, tableNameResolver)} {sqlBuilder.LimitClause(1)}";
            return sql;
        }

        internal static string BuildAnySql<TEntity>(ISqlBuilder sqlBuilder, Expression<Func<TEntity, bool>> predicate, ITableNameResolver tableNameResolver, out DynamicParameters parameters)
        {
            var sql = BuildAnyPredicate(sqlBuilder, typeof(TEntity), tableNameResolver);
            sql += CreateSqlExpression<TEntity>(sqlBuilder)
                .Where(predicate)
                .ToSql(out parameters);
            sql += $" {sqlBuilder.LimitClause(1)}";
            return sql;
        }
    }

}
