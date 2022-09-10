using SehaNotebook.Domain.Entities;
namespace SehaNotebook.API.Services.UserServices
{
   public interface IUserRepo
   {
      Task<User> GetUserById(Guid Id);
      Task<IEnumerable<User>> GetUsers();
      Task<bool> AddUser(User user);
   }
}