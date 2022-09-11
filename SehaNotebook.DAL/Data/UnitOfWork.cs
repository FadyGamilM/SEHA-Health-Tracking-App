using Microsoft.Extensions.Logging;
using SehaNotebook.DAL.IConfiguration;
using SehaNotebook.DAL.IRepository;

namespace SehaNotebook.DAL.Data
{
   public class UnitOfWork : IUnitOfWork, IDisposable
   {
      private readonly AppDbContext _context;
      private readonly ILogger _logger;
      public IUserRepository UserRepository {get; private set;}
      public UnitOfWork(AppDbContext context , ILoggerFactory loggerFactory)
      {
         _context = context;
         _logger = loggerFactory.CreateLogger("DB_Logs"); 
      }

      public async Task CompleteAsyncOperations()
      {
         await _context.SaveChangesAsync();
      }

      public void Dispose()
      {
         // handling memory management by deleting the instance of context we are not using now
         _context.Dispose();
      }
   }
}