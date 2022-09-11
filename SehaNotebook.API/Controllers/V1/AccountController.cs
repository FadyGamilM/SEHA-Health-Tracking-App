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

namespace SehaNotebook.API.Controllers.V1
{
   public class AccountController : BaseController
   {
      private readonly UserManager<IdentityUser> _userManager;
      private readonly JwtConfig _jwtConfig;
      public AccountController(
         IUnitOfWork unitOfWork, 
         UserManager<IdentityUser> userManager,
         IOptionsMonitor<JwtConfig> optionsMonitor
         ) : base(unitOfWork)
      {
         _userManager = userManager;
         // pull the info from the appsetting file 
         _jwtConfig = optionsMonitor.CurrentValue;
      }

      // Register action method
      [HttpPost("register")]
      public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto)
      {
         //! validate the request body
         if(ModelState.IsValid){
            //! check if this email is already registered before
            var registeredBefore = await _userManager.FindByEmailAsync(registerDto.Email);
            if(registeredBefore == null){
               //! create new identity user
               var newUser = new IdentityUser(){
                  Email = registerDto.Email,
                  UserName = registerDto.Email,
                  EmailConfirmed = true
               };
               //! save this identity user into the table
               var isCreated = await _userManager.CreateAsync(newUser, registerDto.Password);
               if(isCreated.Succeeded){
                  //! Create the jwt token
                  var jwtToken = GenerateJwtToken(newUser);
                  //! the response to the user
                  return Ok(
                     new RegisterResponseDto(){
                        Token = jwtToken,
                        Success = true,
                        Errors = new List<string>(){}
                     }
                  );
               }else{
                  return StatusCode(500, new RegisterResponseDto(){
                     Success = false,
                     Token = null,
                     Errors = isCreated.Errors.Select(err => err.Description).ToList()
                  });
               }
            }else{
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
         }else{
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
      //! Method to generate a token and return it 
      private string GenerateJwtToken(IdentityUser user)
      {
         var jwtHandler = new JwtSecurityTokenHandler();
         // get the security key 
         var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
         // token descriptor contains all info required to create a token
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = new ClaimsIdentity(
               new [] {
                  new Claim("Id", user.Id),
                  new Claim(JwtRegisteredClaimNames.Sub, user.Email), // must be uniqe id 
                  new Claim(JwtRegisteredClaimNames.Email, user.Email),
                  //* unique id for each token, [Jti claim is used for refresh token] 
                  new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
               }
            ),
            Expires = DateTime.UtcNow.AddHours(3), // update the expiration time to minutes
            // the algorithm to verify 
            SigningCredentials = new SigningCredentials(
               new SymmetricSecurityKey(key),
               SecurityAlgorithms.HmacSha256Signature 
            )
         };
         var token = jwtHandler.CreateToken(tokenDescriptor);
         var jwtToken = jwtHandler.WriteToken(token);// convert token from object format to string
         return jwtToken;
      }
   }
}