using SehaNotebook.Domain.Entities;

namespace SehaNotebook.DAL.IRepository
{
   // this interface is dedicated for the USER resource only, so if we have any method related 
   // to the user resource, we will add it here, but we will utlize all the crud definitions from 
   // the IGenericRepository to follow the best practices
   public interface IUserRepository : IGenericRepository<User>
   {
      Task<User> GetUserByEmail(string email);
      
      // update user profile information
      Task<bool> UpdateProfileInfo(User user);

      // get user profile info by the identity id 
      Task<User> GetProfileByIdentityId(Guid identityId);
   }
}