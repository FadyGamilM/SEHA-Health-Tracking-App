using SehaNotebook.DAL.IRepository;

namespace SehaNotebook.DAL.IConfiguration
{
   public interface IUnitOfWork
   {
      // Register the first repository
      IUserRepository UserRepository {get;}
      // for save changes
      Task CompleteAsyncOperations();
   }
}