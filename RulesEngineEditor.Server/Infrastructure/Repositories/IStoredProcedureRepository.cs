

namespace RulesEngineEditor.Server.Infrastructure.Repositories
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IStoredProcedureRepository<TEntity, TKey> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TKey id);
        Task<TKey> CreateAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TKey id);
    }

}
