using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using SehaNotebook.DAL.IConfiguration;
using Microsoft.AspNetCore.Identity;

namespace SehaNotebook.API.Controllers.V1
{
   [Route("api/v{version:apiVersion}/")]
   [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme )]
   public class ProfileController : BaseController
   {
      public ProfileController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
      {   }
      
      [HttpGet("profile")]
      public async Task<IActionResult> GetProfile ()
      {
         /*
         to utilize the jwt token to know which user is the owner of this profile
         we need to utilize the _userManager
         */
         var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

         if (loggedInUser == null)
            return BadRequest("User is not found");

         // after making sure that this user exists, we need to retrieve its info from DB [our USERS table]
         var Profile = await _unitOfWork.UserRepository.GetProfileByIdentityId(new Guid(loggedInUser.Id));

         if (Profile == null)
         {
            return BadRequest("User is not found");
         }
         else
         {
            return Ok(Profile);
         }
      }
   }
}