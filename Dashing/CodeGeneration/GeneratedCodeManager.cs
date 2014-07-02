﻿namespace Dashing.CodeGeneration {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using Dapper;

    using Dashing.Engine;

    public class GeneratedCodeManager : IGeneratedCodeManager {
        public CodeGeneratorConfig Config { get; private set; }

        public Assembly GeneratedCodeAssembly { get; private set; }

        private readonly IDictionary<Type, Type> foreignKeyTypes;

        private readonly IDictionary<Type, Type> trackingTypes;

        private readonly IDictionary<Type, Type> updateTypes;

        private readonly IDictionary<Type, Delegate> queryCalls;

        private readonly IDictionary<Type, Delegate> noFetchFkCalls;

        private readonly IDictionary<Type, Delegate> noFetchTrackingCalls;

        private delegate IEnumerable<T> DelegateQuery<T>(SelectWriterResult result, SelectQuery<T> query, IDbConnection conn);

        private delegate T CreateUpdateClass<T>(Type type);

        private readonly IDictionary<Type, Delegate> updateCreators;

        private delegate IEnumerable<T> NoFetchDelegate<out T>(
            IDbConnection conn, 
            string sql, 
            dynamic parameters, 
            IDbTransaction transaction = null, 
            bool buffered = true, 
            int? commandTimeout = null, 
            CommandType? commandType = null);

        public GeneratedCodeManager(CodeGeneratorConfig config, Assembly generatedCodeAssembly) {
            this.Config = config;
            this.GeneratedCodeAssembly = generatedCodeAssembly;

            // go through the defined types and add them
            this.foreignKeyTypes = new Dictionary<Type, Type>();
            this.trackingTypes = new Dictionary<Type, Type>();
            this.updateTypes = new Dictionary<Type, Type>();
            this.queryCalls = new Dictionary<Type, Delegate>();
            this.noFetchFkCalls = new Dictionary<Type, Delegate>();
            this.noFetchTrackingCalls = new Dictionary<Type, Delegate>();
            this.updateCreators = new Dictionary<Type, Delegate>();

            foreach (var type in this.GeneratedCodeAssembly.DefinedTypes) {
                // find the base type from the users code
                if (type.Name.EndsWith(this.Config.ForeignKeyAccessClassSuffix)) {
                    this.foreignKeyTypes.Add(type.BaseType, type);

                    // add the queryCall for this base type
                    // compile dynamic expression for calling Query<T>(SqlWriterResult result, SelectQuery<T> query, IDbConnection conn)
                    // on the generated DapperWrapper
                    var parameters = new List<ParameterExpression> {
                                                                       Expression.Parameter(typeof(SelectWriterResult), "result"), 
                                                                       Expression.Parameter(typeof(SelectQuery<>).MakeGenericType(type.BaseType), "query"), 
                                                                       Expression.Parameter(typeof(IDbConnection), "conn")
                                                                   };
                    var methodCallExpr =
                        Expression.Call(
                            this.GeneratedCodeAssembly.DefinedTypes.First(t => t.Name == "DapperWrapper")
                                .GetMethods()
                                .First(m => m.Name == "Query")
                                .MakeGenericMethod(type.BaseType), 
                            parameters);
                    var queryCall = Expression.Lambda(typeof(DelegateQuery<>).MakeGenericType(type.BaseType), methodCallExpr, parameters).Compile();
                    this.queryCalls.Add(type.BaseType, queryCall);

                    // add the query for no fetches but fk
                    this.MakeNoFetchCall(type, type.BaseType, this.noFetchFkCalls);
                }
                else if (type.Name.EndsWith(this.Config.TrackedClassSuffix)) {
                    this.trackingTypes.Add(type.BaseType.BaseType, type); // tracking classes extend fkClasses
                    this.MakeNoFetchCall(type, type.BaseType.BaseType, this.noFetchTrackingCalls);
                }
                else if (type.Name.EndsWith(this.Config.UpdateClassSuffix)) {
                    this.updateTypes.Add(type.BaseType, type);
                    this.updateCreators.Add(type.BaseType, this.MakeUpdateCreator(type));
                }
            }
        }

        private Delegate MakeUpdateCreator(TypeInfo type) {
            var ctor = Expression.New(type);
            var returnType = typeof(CreateUpdateClass<>).MakeGenericType(type.BaseType);
            var parameters = Expression.Parameter(typeof(Type));
            var lambda = Expression.Lambda(returnType, ctor, parameters);
            return lambda.Compile();
        }

        private void MakeNoFetchCall(TypeInfo type, Type baseType, IDictionary<Type, Delegate> fetchCalls) {
            var noFetchParameters = new List<ParameterExpression> {
                                                                      Expression.Parameter(typeof(IDbConnection), "conn"), 
                                                                      Expression.Parameter(typeof(string), "sql"), 
                                                                      Expression.Parameter(typeof(object), "parameters"), 
                                                                      Expression.Parameter(typeof(IDbTransaction), "tran"), 
                                                                      Expression.Parameter(typeof(bool), "buffered"), 
                                                                      Expression.Parameter(typeof(Nullable<>).MakeGenericType(typeof(int)), "commandTimeout"), 
                                                                      Expression.Parameter(typeof(Nullable<>).MakeGenericType(typeof(CommandType)), "commandType")
                                                                  };
            var noFetchMethodCallExpr = Expression.Call(
                typeof(SqlMapper).GetMethods().First(m => m.Name == "Query" && m.IsGenericMethod).MakeGenericMethod(type), 
                noFetchParameters);
            var noFetchQueryCall = Expression.Lambda(typeof(NoFetchDelegate<>).MakeGenericType(baseType), noFetchMethodCallExpr, noFetchParameters).Compile();
            fetchCalls.Add(baseType, noFetchQueryCall);
        }

        public IEnumerable<T> Query<T>(SelectWriterResult result, SelectQuery<T> query, IDbConnection conn) {
            if (query.HasFetches()) {
                return ((DelegateQuery<T>)this.queryCalls[typeof(T)])(result, query, conn);
            }

            if (query.IsTracked) {
                return this.Tracked(((NoFetchDelegate<T>)this.noFetchTrackingCalls[typeof(T)])(conn, result.Sql, result.Parameters));
            }

            return ((NoFetchDelegate<T>)this.noFetchFkCalls[typeof(T)])(conn, result.Sql, result.Parameters);
        }

        private IEnumerable<T> Tracked<T>(IEnumerable<T> rows) {
            foreach (var row in rows) {
                var trackedEntity = row as ITrackedEntity;
                trackedEntity.IsTracking = true;
                yield return row;
            }
        }

        public int Execute(string sql, IDbConnection conn, dynamic param = null) {
            return conn.Execute(sql, (object)param);
        }

        public Type GetForeignKeyType<T>() {
            return this.foreignKeyTypes[typeof(T)];
        }

        public Type GetTrackingType<T>() {
            return this.trackingTypes[typeof(T)];
        }

        public Type GetUpdateType<T>() {
            return this.updateTypes[typeof(T)];
        }

        public T CreateForeignKeyInstance<T>() {
            return (T)Activator.CreateInstance(this.GetForeignKeyType<T>());
        }

        public T CreateTrackingInstance<T>() {
            return (T)Activator.CreateInstance(this.GetTrackingType<T>());
        }

        public T CreateUpdateInstance<T>() {
            return ((CreateUpdateClass<T>)this.updateCreators[typeof(T)])(typeof(T));
        }

        public void TrackInstance<T>(T entity) {
            ITrackedEntityInspector<T> inspector = new TrackedEntityInspector<T>(entity);
            inspector.ResumeTracking();
        }

        public IEnumerable<T> Query<T>(SqlWriterResult result, IDbConnection conn, bool asTracked = false) {
            if (asTracked) {
                return this.Tracked(((NoFetchDelegate<T>)this.noFetchTrackingCalls[typeof(T)])(conn, result.Sql, result.Parameters));
            }
            else {
                return ((NoFetchDelegate<T>)this.noFetchFkCalls[typeof(T)])(conn, result.Sql, result.Parameters);
            }
        }

        public IEnumerable<T> Query<T>(IDbConnection connection, string sql, dynamic parameters = null) {
            return connection.Query<T>(sql, new DynamicParameters(parameters));
        }
    }
}