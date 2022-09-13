using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SehaNotebook.DAL.Data;
using SehaNotebook.DAL.IConfiguration;
using SehaNotebook.Domain.DTOs;
using SehaNotebook.Domain.Entities;

namespace SehaNotebook.API.Controllers.V1
{

   [Route("api/v{version:apiVersion}/users")]
   // access this resource only if the header contains a token and a valid one 
   [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
   public class UserController : BaseController
   {
      public UserController(IUnitOfWork unitOfWork) : base(unitOfWork)
      {}
      
      [HttpGet("")]
      public async Task<IActionResult> GetUsers ()
      {
         var users = await _unitOfWork.UserRepository.GetAll();
         return Ok(users);
      }

      [HttpGet("{Id:Guid}", Name ="GetUser")]
      public async Task<IActionResult> GetUserById([FromRoute] Guid Id)
      {
         var user = await _unitOfWork.UserRepository.GetById(Id);
         if (user == null){
            return NotFound();
         }else{
            return Ok(user);
         }
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
         userEntity.DateOfBirth = DateTime.UtcNow;
         userEntity.Status = true;
         
         var result = await _unitOfWork.UserRepository.Add(userEntity);
         await _unitOfWork.CompleteAsyncOperations();

         if(result == true){
            return CreatedAtRoute(
               "GetUser",
               new { id = userEntity.Id},
               userEntity
            );
         }else{
            return StatusCode(500, "Error while creating a new user");
         }
      }
   }
}