using SehaNotebook.DAL.IRepository;

namespace SehaNotebook.DAL.IConfiguration
{
   public interface IUnitOfWork
   {
      // Register the first resource repository
      IUserRepository UserRepository {get;}
      // Register the second resource repository
      IRefreshTokenRepository RefreshTokenRepository {get;}
      // for save changes
      Task CompleteAsyncOperations();
   }
}