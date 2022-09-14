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
      // private readonly JwtGenerator _jwtGenerator;
      private readonly TokenValidationParameters _tokenValidationParameters;
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
         _tokenValidationParameters = tokenValidationParameters;
         // _jwtGenerator = new JwtGenerator(optionsMonitor, unitOfWork, tokenValidationParameters, _userManager);
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
                  var jwtToken = await GenerateJwtToken(newUser);
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
                  var jwtToken = await GenerateJwtToken(userExists);
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

      [HttpPost("refreshToken")]
      public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenDto)
      {
         // if request body is valid
         if(ModelState.IsValid)
         {
            // if the token itself is valid ..
            var Result = await VerifyToken(tokenDto);        
            if (Result == null)
            {
               return BadRequest(
                  new AuthResultDto
                  {
                     Success = false,
                     Token = null,
                     Errors = new List<string>(){
                        "token validation has failed"
                     },
                     RefreshToken = null,
                  }
               );
            }
            else 
            {
               return Ok(Result);
            }    
         }
         // if request body is not valid
         else
         {
            return BadRequest(
               new AuthResultDto
               {
                  Success = false,
                  Token = null,
                  Errors = new List<string>(){},
                  RefreshToken = null,
               }
            );
         }
      }

      //! Method to generate a random string
      internal string GenerateRandomStringForRefreshTokenId (int length)
      {
         var random = new Random();
         const string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
         return new string(
            Enumerable.Repeat(charSet, length)
               .Select(s => s[random.Next(s.Length)]).ToArray()
         );
      }
      //! Method to generate a token and return it 
      internal async Task<TokenResponseDto> GenerateJwtToken(IdentityUser user)
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
            Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame),    
            // the algorithm to verify 
            SigningCredentials = new SigningCredentials(
               new SymmetricSecurityKey(key),
               SecurityAlgorithms.HmacSha256Signature 
            )
         };


         var token = jwtHandler.CreateToken(tokenDescriptor);
         
         var jwtToken = jwtHandler.WriteToken(token);// convert token from object format to string
         
         var refreshToken = new RefreshToken
         {
            CreatedDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            Token = $"{GenerateRandomStringForRefreshTokenId(25)}_{Guid.NewGuid()}",
            Status = true,
            IsRevoked = false,
            IsUsed = false,
            Jti = token.Id,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            UserId = user.Id
         };

         await _unitOfWork.RefreshTokenRepository.Add(refreshToken);
         await _unitOfWork.CompleteAsyncOperations();

         return new TokenResponseDto{
            AccessToken = jwtToken,
            RefreshToken = refreshToken.Token
         };
      }
   
      //! Method to verify a token 
      internal async Task<AuthResultDto> VerifyToken(TokenRequestDto tokenDto)
      {
         var tokenHandler = new JwtSecurityTokenHandler();

         try{
            var principal = tokenHandler.ValidateToken(
               tokenDto.AccessToken,
               _tokenValidationParameters,
               out var validatedToken
            );
            //* [1] check the validity of the token by checking if this access token is a valid jwt string not just some characters
            if (validatedToken is JwtSecurityToken jwtSecurityToken){
               //* [2] we need to check if this valid jwt string is generated using the same hashing algorithm
               var result = jwtSecurityToken.Header.Alg.Equals(
                  SecurityAlgorithms.HmacSha256,
                  StringComparison.InvariantCultureIgnoreCase
               );
               if (result == false){
                  Console.WriteLine("############################");
                  Console.WriteLine("the algorithm is not the same");
                  Console.WriteLine("############################");
                  return null;
               }
            }
            //* [3] check the expiry date
            var utcExpiryDate = long.Parse(
               principal.Claims.FirstOrDefault(x=>x.Type==JwtRegisteredClaimNames.Exp).Value
            );
            // convert to date to check
            var expDate = UnixTimeStampToDateTime(utcExpiryDate);
            // if jwt token is not expired, we don't need to refresh it
            if (expDate > DateTime.UtcNow)
            {
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "the access token hasn't expired yet, we can't refresh it"
                  }
               };
            }
            // check if the refresh token exists
            var refreshTokenExists = await _unitOfWork
                                                            .RefreshTokenRepository
                                                            .GetRefreshToken(tokenDto.RefreshToken);
            if (refreshTokenExists == null)
            {
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Invalid Refresh Token"
                  }
               };
            }
            // check the expiry date of the refresh token
            if(refreshTokenExists.ExpiryDate < DateTime.UtcNow)
            {
               // its expired
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Refresh Token has been expired, please login again"
                  }
               };               
            }
            // check if this refresh token has been used before or not
            if(refreshTokenExists.IsUsed)
            {
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Refresh Token has been used before, please login again"
                  }
               };      
            }
            // check if this refresh otken is revoked
            if(refreshTokenExists.IsRevoked)
            {
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Refresh Token has been revoked, it cannot be used"
                  }
               };      
            }
            var jti = principal.Claims.SingleOrDefault(
               x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            if (refreshTokenExists.Jti != jti)
            {
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Refresh token refrence doesn't match the jwt token"
                  }
               };                      
            }
            //! => start processing and get a new token
            // mark this token to be used 
            refreshTokenExists.IsUsed = true;
            var markingAsUsedTokenResult = await _unitOfWork
                                                                           .RefreshTokenRepository
                                                                           .MarkRefreshTokenAsUsed(refreshTokenExists);
            
            if (markingAsUsedTokenResult == true)
            {
               await _unitOfWork.CompleteAsyncOperations();
               // get the user to generate a new tojens for him
               var dbUser = await _userManager.FindByIdAsync(refreshTokenExists.UserId);
               if (dbUser != null)
               {
                  var NewPairsOfTokens = await GenerateJwtToken(dbUser);
                  // return the new pair of tokens :D
                  return new AuthResultDto
                  {
                     Token = NewPairsOfTokens.AccessToken,
                     RefreshToken = NewPairsOfTokens.RefreshToken,
                     Success = true,
                     Errors = new List<string>(){}
                  };
               }
               else
                  return new AuthResultDto
                  {
                     Success = false,
                     Errors = new List<string>()
                     {
                        "Error while processing the request, there is no user related to this token"
                     }
                  };   
            }
            else
            {
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Error while processing the reques5"
                  }
               };    
            }
         }
         catch(Exception ex){
            Console.WriteLine("************************************");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("************************************");
            return null;
         }
      }
      internal DateTime UnixTimeStampToDateTime(long expDate)
      {
         var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
         dateTime = dateTime.AddSeconds(expDate).ToUniversalTime();
         return dateTime;

      }

   }
}