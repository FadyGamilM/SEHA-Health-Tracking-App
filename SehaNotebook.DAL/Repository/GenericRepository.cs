using SehaNotebook.DAL.IRepository;
using SehaNotebook.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SehaNotebook.DAL.Repository
{
   public class GenericRepository<T> : IGenericRepository<T> where T : class
   {
      protected ILogger _logger;
      protected AppDbContext _context;
      internal DbSet<T> _dbSet;
      public GenericRepository(AppDbContext context, ILogger logger)
      {
         _logger = logger;
         _context = context;
         _dbSet = _context.Set<T>();
      }
      //! => Create new entity
      public virtual async Task<bool> Add(T entity)
      {
         await _dbSet.AddAsync(entity);
         return true;
      }
      //! => Delete existing entity
      public virtual async Task<bool> Delete(Guid Id, string userId)
      {
         throw new NotImplementedException();
      }
      //! => Get all entites (will be overrided)
      public virtual async Task<IEnumerable<T>> GetAll()
      {
         return await _dbSet.ToListAsync();
      }
      //! => Get specific entity 
      public virtual async Task<T> GetById(Guid Id)
      {
         return await _dbSet.FindAsync(Id);
      }

      public Task<bool> Upsert(T entity)
      {
         throw new NotImplementedException();
      }
   }
}