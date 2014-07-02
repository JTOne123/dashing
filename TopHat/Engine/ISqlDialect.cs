namespace TopHat.Engine {
    using System.Text;

    using TopHat.Configuration;

    public interface ISqlDialect {
        void AppendQuotedTableName(StringBuilder sql, IMap map);

        void AppendQuotedName(StringBuilder sql, string name);

        void AppendColumnSpecification(StringBuilder sql, IColumn column);

        void AppendEscaped(StringBuilder sql, string s);

        string WriteDropTableIfExists(string tableName);

        string GetIdSql();

        /// <summary>
        /// Applies paging to the sql query
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="take"></param>
        /// <param name="skip"></param>
        /// <remarks>The sql command will be past the parameters @take and @skip so those names should be used</remarks>
        void ApplyPaging(StringBuilder sql, StringBuilder orderClause, int take, int skip);
    }
}