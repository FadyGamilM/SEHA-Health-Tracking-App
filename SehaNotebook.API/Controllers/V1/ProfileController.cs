using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using SehaNotebook.DAL.IConfiguration;
using Microsoft.AspNetCore.Identity;
using SehaNotebook.Domain.DTOs;

namespace SehaNotebook.API.Controllers.V1
{
   [Route("api/v{version:apiVersion}/profile")]
   [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme )]
   public class ProfileController : BaseController
   {
      public ProfileController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
      {   }
      
      [HttpGet("")]
      public async Task<IActionResult> GetProfile ()
      {
         /*
         to utilize the jwt token to know which user is the owner of this profile
         we need to utilize the _userManager
         */
         var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

         if (loggedInUser == null)
            return NotFound("User is not found");

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

      [HttpPut("")]
      public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
      {
         var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

         if (loggedInUser == null) return NotFound();

         var profile = await _unitOfWork.UserRepository.GetProfileByIdentityId(new Guid(loggedInUser.Id));

         if(profile == null) return NotFound();
         if (dto.Address != null) 
            profile.Address = dto.Address;
         if (dto.Phone != null)
            profile.Phone = dto.Phone;
         if (dto.Country != null)
            profile.Country = dto.Country;
         if (dto.Sex != null)
            profile.Sex = dto.Sex;

         // update the info inside the DB
         var updateResult = await _unitOfWork.UserRepository.UpdateProfileInfo(profile);
         if (updateResult == true) 
         {
            await _unitOfWork.CompleteAsyncOperations();
            return Ok(profile);
         }
         else return StatusCode(500, "Error while updating your inforimations");
      }
   }
}