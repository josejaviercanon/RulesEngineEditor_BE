

namespace RulesEngineEditor.Server.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;

    public abstract class BaseStoredProcedureRepository<TEntity, TKey> : IStoredProcedureRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly DbContext Context;
        protected readonly DbSet<TEntity> DbSet;

        protected BaseStoredProcedureRepository(DbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = context.Set<TEntity>();
        }

        // Abstract naming patterns the AI MUST provide for its targeted entity
        protected abstract string SpGetAll { get; }
        protected abstract string SpGetById { get; }
        protected abstract string SpInsert { get; }
        protected abstract string SpUpdate { get; }
        protected abstract string SpDelete { get; }
        protected abstract string IdParameterName { get; }

        // Abstract parameter mappers the AI agent must implement per-table
        protected abstract SqlParameter[] MapToInsertParameters(TEntity entity, SqlParameter outputIdParam);
        protected abstract SqlParameter[] MapToUpdateParameters(TEntity entity);

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await DbSet
                .FromSqlRaw($"EXEC {SpGetAll}")
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(TKey id)
        {
            var idParam = new SqlParameter(IdParameterName, id ?? throw new ArgumentNullException(nameof(id)));

            return await DbSet
                .FromSqlRaw($"EXEC {SpGetById} {IdParameterName}", idParam)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public virtual async Task<TKey> CreateAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Deterministically handle identity generation catching via SQL output parameters
            var outputIdParam = new SqlParameter("@NewId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var parameters = MapToInsertParameters(entity, outputIdParam);

            // Build command string e.g., "EXEC sp_InsertCustomer @Param1, @Param2, @NewId OUTPUT"
            var paramPlaceholders = new List<string>();
            foreach (var p in parameters)
            {
                paramPlaceholders.Add(p.Direction == ParameterDirection.Output ? $"{p.ParameterName} OUTPUT" : p.ParameterName);
            }
            string sql = $"EXEC {SpInsert} {string.Join(", ", paramPlaceholders)}";

            await Context.Database.ExecuteSqlRawAsync(sql, parameters);

            return (TKey)outputIdParam.Value;
        }

        public virtual async Task UpdateAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var parameters = MapToUpdateParameters(entity);
            var paramPlaceholders = new List<string>();
            foreach (var p in parameters) paramPlaceholders.Add(p.ParameterName);

            string sql = $"EXEC {SpUpdate} {string.Join(", ", paramPlaceholders)}";

            await Context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        public virtual async Task DeleteAsync(TKey id)
        {
            var idParam = new SqlParameter(IdParameterName, id ?? throw new ArgumentNullException(nameof(id)));
            string sql = $"EXEC {SpDelete} {IdParameterName}";

            await Context.Database.ExecuteSqlRawAsync(sql, idParam);
        }
    }

}
