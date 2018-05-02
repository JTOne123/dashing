﻿namespace Dashing.Tests.Engine.Dialects {
    using System.Data;
    using System.Text;

    using Dashing.Configuration;
    using Dashing.Engine.Dialects;

    using Xunit;

    public class SqlServerDialectBaseTests {
        [Fact]
        public void UsesSqlServerEscapes() {
            var sb = new StringBuilder();
            this.MakeTarget().AppendQuotedName(sb, "foo");

            Assert.Equal("[foo]", sb.ToString());
        }

        [Fact]
        public void UsesSqlServerEscapeForTables() {
            var sb = new StringBuilder();
            var map = new Map<int> { Table = "foo" };
            this.MakeTarget().AppendQuotedTableName(sb, map);

            Assert.Equal("[foo]", sb.ToString());
        }

        [Fact]
        public void UsesSqlServerEscapeForColumns() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.Int32, IsNullable = true, Map = new Map<int>() };
            this.MakeTarget().AppendColumnSpecification(sb, col);

            Assert.Equal("[foo] int null", sb.ToString());
        }

        [Fact]
        public void BooleanColumnHasBitType() {
            var actual = this.GetColumnSpec(new Column<int> { DbName = "foo", DbType = DbType.Boolean, Map = new Map<int>() });
            Assert.Equal("[foo] bit not null default (0)", actual);
        }

        [Fact]
        public void DateTime2ColumnHasDateTime2Type() {
            var actual = this.GetColumnSpec(new Column<int> { DbName = "foo", DbType = DbType.DateTime2, Precision = 5, Map = new Map<int>() });
            Assert.Equal("[foo] datetime2(5) not null default (current_timestamp)", actual);
        }

        [Fact]
        public void GuidColumnHasUniqueidentifierType() {
            var actual = this.GetColumnSpec(new Column<int> { DbName = "foo", DbType = DbType.Guid, Map = new Map<int>() });
            Assert.Equal("[foo] uniqueidentifier not null default (newid())", actual);
        }

        [Fact]
        public void ObjectColumnHasSqlVariantType() {
            var actual = this.GetColumnSpec(new Column<int> { DbName = "foo", DbType = DbType.Object, Map = new Map<int>() });
            Assert.Equal("[foo] sql_variant not null", actual);
        }

        [Fact]
        public void AutoGeneratedColumnHasIdentityClause() {
            var actual = this.GetColumnSpec(new Column<int> { DbName = "foo", DbType = DbType.Int32, IsAutoGenerated = true, Map = new Map<int>() });
            Assert.Equal("[foo] int not null identity(1,1)", actual);
        }

        private string GetColumnSpec(IColumn col) {
            var sb = new StringBuilder();
            this.MakeTarget().AppendColumnSpecification(sb, col);
            return sb.ToString();
        }

        private SqlServerDialect MakeTarget() {
            return new SqlServerDialect();
        }
    }
}