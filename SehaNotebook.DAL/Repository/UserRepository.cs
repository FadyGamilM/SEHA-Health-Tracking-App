using SehaNotebook.Domain.Entities;
using SehaNotebook.DAL.IRepository;
using SehaNotebook.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SehaNotebook.DAL.Repository
{
   public class UserRepository : GenericRepository<User>, IUserRepository
   {
      public UserRepository(AppDbContext context, ILogger logger) : base(context, logger)
      {
      }

      //! concrete implementation of GetAll() method which is one of the general CRUD inherited from the GenericRepository
      public override async Task<IEnumerable<User>> GetAll()
      {
         try{
            // utilize the _dbSet from the GenericRepository class
            var users = await _dbSet
                                          .Where(user => user.Status == true)
                                          .AsNoTracking()
                                          .ToListAsync();
            return users;
         }catch(Exception exeption){
            // utilize the _logger from the GenericRepository class
            _logger.LogError(exeption, $"'{typeof(UserRepository)}' : [GetAll] API has generated an error");
            return new List<User>();
         }
      }


      //! concrete implementation of GetUserByEmail() method which is resourcespecific related method from IUserRepository
      public async Task<User> GetUserByEmail(string email)
      {
         try{
            // utilize the _dbSet from the GenericRepository class
            var user = await _dbSet
                                          .Where(user => user.Email == email)
                                          .AsNoTracking()
                                          .SingleOrDefaultAsync();
            return user;
         }catch(Exception exeption){
            // utilize the _logger from the GenericRepository class
            _logger.LogError(exeption, $"'{typeof(UserRepository)}' : [GetUserByEmail] API has generated an error");
            return null;
         }
      }
   }
}