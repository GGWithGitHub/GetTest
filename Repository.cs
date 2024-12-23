using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PS.DataCore.Core
{
    public class Repository<TEntity, TContext> : IRepository<TEntity, TContext>
     where TEntity : class
     where TContext : DbContext
    {

        protected readonly DbSet<TEntity> dbSet;
        protected readonly TContext dbContext;

        public Repository(TContext dbContext)
        {
            this.dbContext = dbContext;
            dbSet = this.dbContext.Set<TEntity>();
        }

        public TContext DbContext => dbContext;

        public virtual ValueTask<TEntity> GetByIdAsync(object id)
        {
            return dbSet.FindAsync(id);
        }

        public virtual TEntity GetById(object id)
        {
            return dbSet.Find(id);
        }

        public virtual IQueryable<TEntity> GetAll(bool disableTracking = false)
        {
            IQueryable<TEntity> query = null;
            if (disableTracking)
            {
                query = dbSet.AsNoTracking().AsQueryable();
            }
            else
            {
                query = dbSet.AsQueryable();
            }
            return query;
        }

        public virtual TContext GetContext()
        {
            return this.dbContext;
        }

        public virtual void Create(TEntity entity)
        {
            dbSet.Add(entity);
        }

        /// <summary>
        /// Does a hard delete of the entity.  Override this if you want to soft delete instead.
        /// </summary>
        public virtual void Delete(TEntity entity)
        {
            dbSet.Remove(entity);
        }
        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            dbSet.RemoveRange(entities);
        }


        public void RollBack()
        {
            var changedEntries = dbContext.ChangeTracker.Entries<TEntity>()
                .Where(x => x.State != EntityState.Unchanged)
                .ToList();

            foreach (var entry in changedEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }
        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            dbSet.RemoveRange(entities);
        }

        public virtual Task BulkInsertAsync(IList<TEntity> entities)
        {
            if (dbContext.Database.IsSqlServer())
            {
                return dbContext.BulkInsertAsync<TEntity>(entities);
            }
            else
            {
                foreach (TEntity entity in entities)
                {
                    Add(entity);
                }
                return dbContext.SaveChangesAsync();
            }
        }

        public virtual Task BulkDeleteAsync(IList<TEntity> entities)
        {
            if (dbContext.Database.IsSqlServer())
            {
                return dbContext.BulkDeleteAsync<TEntity>(entities);
            }
            else
            {
                foreach (TEntity entity in entities)
                {
                    Delete(entity);
                }
                return dbContext.SaveChangesAsync();
            }
        }

        public IQueryable<TEntity> Query(string sql, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public TEntity Search(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public TEntity Single(Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false)
        {
            throw new NotImplementedException();
        }

        public void Add(TEntity entity)
        {
            dbContext.Add<TEntity>(entity);
        }

        public void Add(params TEntity[] entities)
        {
            entities.ToList().ForEach(x => Add(x));
        }

        public void Add(IEnumerable<TEntity> entities)
        {
            entities.ToList().ForEach(x => Add(x));
        }

        public void Delete(object id)
        {
            var entity = GetById(id);
            Delete(entity);
        }

        public void Delete(params TEntity[] entities)
        {
            throw new NotImplementedException();
        }

        public void Delete(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }

        public void Update(TEntity entity)
        {
            dbContext.Update<TEntity>(entity);
        }

        public void Update(params TEntity[] entities)
        {
            entities.ToList().ForEach(x => Update(x));
        }

        public void Update(IEnumerable<TEntity> entities)
        {
            entities.ToList().ForEach(x => Update(x));
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }

        public IQueryable<TEntity> Filter(Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false)
        {
            IQueryable<TEntity> query = null;
            if (disableTracking)
            {
                query = dbSet.AsNoTracking().AsQueryable();
            } else
            {
                query = dbSet.AsQueryable();
            }

            query = query.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);

            if (include != null)
                query = include(query);

            return query;
        }

        public Task<List<TEntity>> FilterAsync(Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false)
        {
            return Filter(predicate, orderBy, include, disableTracking).ToListAsync();
        }
    }

    public interface IRepository<TEntity, TContext> : IDisposable
        where TEntity : class
        where TContext : DbContext
    {
        TContext DbContext { get; }

        IQueryable<TEntity> Query(string sql, params object[] parameters);

        TEntity Search(params object[] keyValues);

        TEntity Single(Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false);

        IQueryable<TEntity> Filter(Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false);

        Task<List<TEntity>> FilterAsync(Expression<Func<TEntity, bool>> predicate = null,
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
           bool disableTracking = false);
        void Add(TEntity entity);
        void Add(params TEntity[] entities);
        void Add(IEnumerable<TEntity> entities);

        void Delete(TEntity entity);
        void Delete(object id);
        void Delete(params TEntity[] entities);
        void Delete(IEnumerable<TEntity> entities);
        void DeleteRange(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void Update(params TEntity[] entities);
        void Update(IEnumerable<TEntity> entities);
        ValueTask<TEntity> GetByIdAsync(object id);
        TEntity GetById(object id);
        void Create(TEntity entity);
        Task BulkInsertAsync(IList<TEntity> entities);
        Task BulkDeleteAsync(IList<TEntity> entities);
        IQueryable<TEntity> GetAll(bool disableTracking = false);
        TContext GetContext();
        void RollBack();
        void RemoveRange(IEnumerable<TEntity> entities);
    }
}
