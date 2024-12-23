using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PS.DataCore.Models;
using StoredProcedureEFCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PS.DataCore.Core
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
    {
        private readonly TContext dbContext;

        public UnitOfWork(TContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public int Commit()
        {
            return dbContext.SaveChanges();
        }

        public Task<int> CommitAsync()
        {
            return dbContext.SaveChangesAsync();
        }

        public async Task<int> CompleteAsync(int contactId = 0)
        {
            var entities = dbContext.ChangeTracker.Entries()
                .Where(e => e.Entity is EntityBase && (e.State == EntityState.Added || e.State == EntityState.Modified))
                .Select(e => e.Entity as EntityBase);

            foreach (var entity in entities)
            {
                entity?.SaveStandardFields(contactId); // replace 0 with your contactID
            }
            try
            {
                return await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception
                throw new Exception("An error occurred while saving changes.", ex.InnerException);
                throw;
            }
        }

        public void Rollback()
        {
            dbContext.Database.RollbackTransaction();
        }

        public IDbContextTransaction BeginTransaction()
        {
            return dbContext.Database.BeginTransaction(System.Data.IsolationLevel.Snapshot);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Snapshot);
        }

        public async Task<IList<TOutput>> ExecuteStoredProcedureQueryAsync<TOutput>(string storedProcedure,
            SqlParameter[] parameters = null) where TOutput : class, new()
        {
            IList<TOutput> rows = null;
            var sp = dbContext.LoadStoredProc(storedProcedure);
            if (parameters != null && parameters.Any())
            {
                foreach (var parameter in parameters)
                {
                    if ((parameter.DbType == DbType.DateTime || parameter.DbType == DbType.DateTime2) && parameter.Value != null)
                    {
                        sp.AddParam<DateTime>(parameter.ParameterName, (DateTime)parameter.Value);
                    }
                    else
                    {
                        sp.AddParam<string>(parameter.ParameterName, parameter.Value?.ToString());
                    }
                }
            }

            await sp.ExecAsync(async r => rows = await r.ToListAsync<TOutput>());

            return rows;
        }
        public async Task<TOutput> ExecuteScalarStoredProcedureQueryAsync<TOutput>(string storedProcedure,
            SqlParameter[] parameters = null)
        {
            var sp = dbContext.LoadStoredProc(storedProcedure);
            if (parameters != null && parameters.Any())
            {
                foreach (var parameter in parameters)
                {
                    sp.AddParam<string>(parameter.ParameterName, parameter.Value.ToString());
                }
            }
            TOutput result = default(TOutput);
            await sp.ExecScalarAsync<TOutput>(r => result = r);

            return result;
        }        

        public async Task ExecuteNonQueryAsync(string storedProcedure, SqlParameter[] parameters = null)
        {
            var sp = dbContext.LoadStoredProc(storedProcedure);
            
            if (parameters != null && parameters.Any())
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.Value != null)
                    {
                        sp.AddParam<string>(parameter.ParameterName, parameter.Value.ToString());
                    } else
                    {
                        sp.AddParam(parameter);
                    }
                }
            }
            await sp.ExecNonQueryAsync();
        }

        public async Task ExecNonQueryWithOutputPraramAsync(string storedProcedure, SqlParameter[] parameters)
        {
            var sp = dbContext.LoadStoredProc(storedProcedure);

            if (parameters != null && parameters.Any())
            {
                foreach (var parameter in parameters)
                {
                    sp.AddParam(parameter);
                }
            }
            await sp.ExecNonQueryAsync();
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }
        public DbContext GetContext()
        {
            return dbContext;
        }
    }

    public interface IUnitOfWork<TDbContext> : IDisposable where TDbContext : DbContext
    {
        int Commit();
        Task<int> CommitAsync();
        Task<int> CompleteAsync(int contactId = 0);
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<IList<TOutput>> ExecuteStoredProcedureQueryAsync<TOutput>(string storedProcedure,
            params SqlParameter[] parameters) where TOutput : class, new();
        Task<TOutput> ExecuteScalarStoredProcedureQueryAsync<TOutput>(string storedProcedure,
           SqlParameter[] parameters = null);
        Task ExecuteNonQueryAsync(string storedProcedure, SqlParameter[] parameters = null);
        void Rollback();
        Task ExecNonQueryWithOutputPraramAsync(string storedProcedure, SqlParameter[] parameters);
        DbContext GetContext();
    }
}
