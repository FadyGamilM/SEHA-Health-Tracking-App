using System.Text;
using Microsoft.AspNetCore.Identity;
using SehaNotebook.Authentication.Configurations;
using SehaNotebook.DAL.IConfiguration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using SehaNotebook.Authentication.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using SehaNotebook.Domain.Entities;
using SehaNotebook.API.Helpers;

namespace SehaNotebook.API.Controllers.V1
{
   [Route("api/v{version:apiVersion}/accounts")]
   public class AccountController : BaseController
   {
      //! Inject the userManager to add the user to the AspNetUsers relation
      private readonly UserManager<IdentityUser> _userManager;
      //! Inject the jwtConfig to get the configs from appsettings
      private readonly JwtConfig _jwtConfig;
      //! Inject the Token Validation Parametr singelton service here 
      private readonly TokenValidationParameters _tokenValidationParameter;
      private readonly JwtGenerator _jwtGenerator;
      //! Depedency Injection 
      public AccountController(
         IUnitOfWork unitOfWork, 
         UserManager<IdentityUser> userManager,
         IOptionsMonitor<JwtConfig> optionsMonitor,
         TokenValidationParameters tokenValidationParameters
         ) : base(unitOfWork)
      {
         _userManager = userManager;
         // pull the info from the appsetting file 
         _jwtConfig = optionsMonitor.CurrentValue;
         _tokenValidationParameter = tokenValidationParameters;
         _jwtGenerator = new JwtGenerator(optionsMonitor, unitOfWork);
      }

      //! Register action method
      [HttpPost("register")]
      public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto)
      {
         //! validate the request body
         if(ModelState.IsValid){
            //! check if this email is already registered before
            var registeredBefore = await _userManager.FindByEmailAsync(registerDto.Email);
            if(registeredBefore == null){
               //! create new identity user to add it to the AspNetUsers relation
               var newUser = new IdentityUser(){
                  // the identityUser generate a Guid in string format as an Id
                  Email = registerDto.Email,
                  UserName = registerDto.Email,
                  EmailConfirmed = true
               };
               //! create a new instance from our custom Users table to add to Users relation
               var user = new User(){
                  IdentityId = new Guid(newUser.Id),
                  FirstName = registerDto.FirstName,
                  LastName = registerDto.LastName,
                  Email = registerDto.Email,
                  DateOfBirth = DateTime.UtcNow,
                  Phone = "",
                  Country = "",
                  Status = true,
               };
               await _unitOfWork.UserRepository.Add(user);
               await _unitOfWork.CompleteAsyncOperations();
               //! save this identity user into the table
               var isCreated = await _userManager.CreateAsync(newUser, registerDto.Password);
               if(isCreated.Succeeded){
                  //! Create the jwt token
                  var jwtToken = await _jwtGenerator.GenerateJwtToken(newUser);
                  //! the response to the user
                  return Ok(
                     new RegisterResponseDto(){
                        Token = jwtToken.AccessToken,
                        RefreshToken = jwtToken.RefreshToken,
                        Success = true,
                        Errors = new List<string>(){}
                     }
                  );
               }
               //! if something go wrong while saving this new user to the database
               else{
                  return StatusCode(500, new RegisterResponseDto(){
                     Success = false,
                     Token = null,
                     Errors = isCreated.Errors.Select(err => err.Description).ToList()
                  });
               }
            }
            //! if this email is registered before
            else{
               return BadRequest(
                  new RegisterResponseDto(){
                     Success = false,
                     Errors = new List<string>(){
                        "This email is already registered"
                     },
                     Token=null
                  }
               );
            }
         }
         //! if the request body is not valid..
         else{
            return BadRequest(
               new RegisterResponseDto(){
                  Token = null,
                  Success = false,
                  Errors = new List<string>(){
                     "Invalid Request"
                  }
               }
            );
         }
      }

      [HttpPost("login")]
      public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
      {
         //! check the request body if its valid
         if (ModelState.IsValid)
         {
            //! check if there is a user with this email in our DB
            var userExists = await _userManager.FindByEmailAsync(loginDto.Email);
            if (userExists != null)
            {
               //! check if password is correct
               var passMatches = await _userManager.CheckPasswordAsync(userExists, loginDto.Password);
               if (passMatches == true)
               {
                  //! Generate the jwt token
                  var jwtToken = await _jwtGenerator.GenerateJwtToken(userExists);
                  return Ok(
                     new LoginResponseDto(){
                        Success =true,
                        Token = jwtToken.AccessToken,
                        RefreshToken = jwtToken.RefreshToken,
                        Errors = new List<string>(){}
                     }
                  );
               }
               else
               {
                  return BadRequest(
                     new LoginResponseDto(){
                        Success =false,
                        Token = null,
                        Errors = new List<string>(){
                        "Invalid Credentials"
                        }
                     }
                  );
               }
            }
            else
            {
              return BadRequest(
                     new LoginResponseDto(){
                        Success =false,
                        Token = null,
                        Errors = new List<string>(){
                        "Invalid Credentials"
                        }
                     }
                  );
            }
         }
         //! if body is not valid request body
         else
         {
            return BadRequest(
               new LoginResponseDto(){
                  Success =false,
                  Token = null,
                  Errors = new List<string>(){
                     "Invalid Request"
                  }
               }
            );
         }
      }



   }
}