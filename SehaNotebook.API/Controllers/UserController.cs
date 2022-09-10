using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SehaNotebook.API.Services.UserServices;
using SehaNotebook.DAL.Data;
using SehaNotebook.Domain.DTOs;
using SehaNotebook.Domain.Entities;

namespace SehaNotebook.API.Controllers
{
   [ApiController]
   [Route("api/users")]
   public class UserController : ControllerBase
   {
      private readonly IUserRepo _userRepo;
      private readonly AppDbContext _context;
      public UserController(IUserRepo userRepo, AppDbContext context)
      {
         _userRepo = userRepo;
         _context = context;
      }
      
      [HttpGet("")]
      public async Task<IActionResult> GetUsers ()
      {
         var users = await this._userRepo.GetUsers();
         return Ok(users);
      }

      [HttpPost]
      public async Task<IActionResult> AddUser([FromBody]CreateUserDto user)
      {
         var userEntity = new User();
         userEntity.FirstName = user.FirstName;
         userEntity.LastName = user.LastName;
         userEntity.Country = user.Country;
         userEntity.Email = user.Email;
         userEntity.Phone = user.Phone;
         userEntity.DateOfBirth = Convert.ToDateTime(user.DateOfBirth);
         userEntity.Status = true;
         var result = await this._userRepo.AddUser(userEntity);
         if(result == true){
            return Ok("Created");
         }else{
            return StatusCode(500, "Error while creating a new user");
         }
      }
   }
}