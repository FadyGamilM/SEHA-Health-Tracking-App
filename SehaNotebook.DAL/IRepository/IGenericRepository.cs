namespace SehaNotebook.DAL.IRepository
{
   public interface IGenericRepository<T> where T : class
   {
      // get all entities
      Task<IEnumerable<T>> GetAll();
      // get specific entity by id
      Task<T> GetById(Guid Id);
      // create new entity
      Task<bool> Add(T entity);
      Task<bool> Delete(Guid Id, string userId);
      // Upsert => update or insert if it doesn't exists
      Task<bool> Upsert(T entity);
   }
}