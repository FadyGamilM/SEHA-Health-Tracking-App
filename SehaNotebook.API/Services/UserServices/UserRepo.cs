using Microsoft.EntityFrameworkCore;
using SehaNotebook.DAL.Data;
using SehaNotebook.Domain.Entities;

namespace SehaNotebook.API.Services.UserServices
{
   public class UserRepo : IUserRepo
   {
      private readonly AppDbContext _context;
      public UserRepo(AppDbContext context)
      {
         _context = context;         
      }
      public async Task<User> GetUserById(Guid Id)
      {
         var user = await _context
                                       .Users
                                       .Where(u => u.Status == true)
                                       .SingleOrDefaultAsync();
         return user;
      }

      public async Task<IEnumerable<User>> GetUsers()
      {
         var users = await _context.Users.Where(user => user.Status == true).ToListAsync();
         return users;
      }

      public async Task<bool> AddUser(User user)
      {
         await _context.Users.AddAsync(user);
         var result = _context.SaveChanges();
         if ((int) result > 0){
            return true;
         }else{
            return false;
         }
      }
   }
}