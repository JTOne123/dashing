namespace Dashing.CodeGeneration {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class TrackedEntityInspector<T> : ITrackedEntityInspector<T> where T : ITrackedEntity {
        private readonly T trackedEntity;

        public TrackedEntityInspector(T trackedEntity) {
            this.trackedEntity = trackedEntity;
        }

        public bool IsPropertyDirty<TResult>(Expression<Func<T, TResult>> propertyExpression) {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null) {
                throw new ArgumentException("mapToExpression must be a MemberExpression");
            }

            return this.GetDirtyProperties().Contains(memberExpression.Member.Name);
        }

        public TResult GetOldValue<TResult>(Expression<Func<T, TResult>> propertyExpression) {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null) {
                throw new ArgumentException("mapToExpression must be a MemberExpression");
            }

            return (TResult)this.GetOldValue(memberExpression.Member.Name);
        }

        public void EnableTracking() {
            this.trackedEntity.EnableTracking();
        }

        public void DisableTracking() {
            this.trackedEntity.DisableTracking();
        }

        public bool IsTrackingEnabled() {
            return this.trackedEntity.IsTrackingEnabled();
        }

        public IEnumerable<string> GetDirtyProperties() {
            return this.trackedEntity.GetDirtyProperties();
        }

        public object GetOldValue(string propertyName) {
            return this.trackedEntity.GetOldValue(propertyName);
        }
    }
}