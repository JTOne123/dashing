﻿namespace Dashing.Engine.DML {
    using System;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Text;

    using Dashing.Configuration;
    using Dashing.Engine.Dialects;

    internal class OrderClauseWriter : MemberExpressionFetchNodeVisitor, IOrderClauseWriter {
        private readonly IConfiguration configuration;

        private readonly ISqlDialect dialect;

        public OrderClauseWriter(IConfiguration configuration, ISqlDialect dialect) {
            this.configuration = configuration;
            this.dialect = dialect;
        }

        public string GetOrderClause<T>(OrderClause<T> clause, FetchNode rootNode, out bool isRootPrimaryKeyClause) {
            return this.GetOrderClauseInner(clause, rootNode, null, null, out isRootPrimaryKeyClause);
        }

        public string GetOrderClause<T>(
            OrderClause<T> clause,
            FetchNode rootNode,
            Func<IColumn, FetchNode, string> aliasRewriter,
            Func<IColumn, FetchNode, string> nameRewriter,
            out bool isRootPrimaryKeyClause) {
            if (aliasRewriter == null) {
                throw new ArgumentNullException("aliasRewriter");
            }

            if (nameRewriter == null) {
                throw new ArgumentNullException("nameRewriter");
            }

            return this.GetOrderClauseInner(clause, rootNode, aliasRewriter, nameRewriter, out isRootPrimaryKeyClause);
        }

        private string GetOrderClauseInner<T>(
            OrderClause<T> clause,
            FetchNode rootNode,
            Func<IColumn, FetchNode, string> aliasRewriter,
            Func<IColumn, FetchNode, string> nameRewriter,
            out bool isRootPrimaryKeyClause) {
            var lambdaExpression = clause.Expression as LambdaExpression;
            if (lambdaExpression == null) {
                throw new InvalidOperationException("OrderBy clauses must be LambdaExpressions");
            }

            var node = this.VisitExpression(lambdaExpression.Body, rootNode);
            var sb = new StringBuilder();
            if (node == null) {
                var column = this.configuration.GetMap<T>().Columns[((MemberExpression)lambdaExpression.Body).Member.Name];
                this.dialect.AppendQuotedName(sb, nameRewriter != null ? nameRewriter(column, node) : column.DbName);
                sb.Append(" ").Append(clause.Direction == ListSortDirection.Ascending ? "asc" : "desc");
                isRootPrimaryKeyClause = column.IsPrimaryKey;
            }
            else {
                IColumn column = null;
                if (ReferenceEquals(node, rootNode)) {
                    column = this.configuration.GetMap<T>().Columns[((MemberExpression)lambdaExpression.Body).Member.Name];
                    isRootPrimaryKeyClause = column.IsPrimaryKey;
                }
                else {
                    if (node.Column.Relationship == RelationshipType.ManyToOne) {
                        column = node.Column.ParentMap.Columns[((MemberExpression)lambdaExpression.Body).Member.Name];
                    }
                    else if (node.Column.Relationship == RelationshipType.OneToOne) {
                        column = node.Column.OppositeColumn.Map.Columns[((MemberExpression)lambdaExpression.Body).Member.Name];
                    }
                    else {
                        throw new NotSupportedException();
                    }

                    isRootPrimaryKeyClause = false;
                }

                sb.Append(aliasRewriter != null ? aliasRewriter(column, node) : node.Alias).Append(".");
                this.dialect.AppendQuotedName(sb, nameRewriter != null ? nameRewriter(column, node) : column.DbName);
                sb.Append(" ").Append(clause.Direction == ListSortDirection.Ascending ? "asc" : "desc");
            }

            return sb.ToString();
        }
    }
}