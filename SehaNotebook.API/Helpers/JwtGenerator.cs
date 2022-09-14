using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SehaNotebook.Authentication.Configurations;
using SehaNotebook.Authentication.DTOs;
using SehaNotebook.DAL.IConfiguration;
using SehaNotebook.Domain.Entities;

namespace SehaNotebook.API.Helpers
{
   public class JwtGenerator
   {
      private readonly JwtConfig _jwtConfig;
      private readonly IUnitOfWork _unitOfWork;
      //! Inject the Token Validation Parametr singelton service here 
      private readonly TokenValidationParameters _tokenValidationParameter;
      private readonly UserManager<IdentityUser> _userManager;
      public JwtGenerator(
         IOptionsMonitor<JwtConfig> optionsMonitor, 
         IUnitOfWork unitOfWork, 
         TokenValidationParameters tokenValidationParameters,
         UserManager<IdentityUser> userManager      )
      {
         _jwtConfig = optionsMonitor.CurrentValue;
         _unitOfWork = unitOfWork;
         _tokenValidationParameter = tokenValidationParameters;
         _userManager = userManager;
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
               _tokenValidationParameter,
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
            var refreshTokenExists = await _unitOfWork.RefreshTokenRepository.GetRefreshToken(tokenDto.RefreshToken);
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
            var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            if (refreshTokenExists.Jti != jti){
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
            var markingAsUsedTokenResult = await _unitOfWork.RefreshTokenRepository.MarkRefreshTokenAsUsed(refreshTokenExists);
            
            if (markingAsUsedTokenResult == true)
            {
               await _unitOfWork.CompleteAsyncOperations();
               // get the user to generate a new tojens for him
               var dbUser = await _userManager.FindByEmailAsync(refreshTokenExists.UserId);
               if (dbUser == null)
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
                        "Error while processing the reques5"
                     }
                  };   
            }
            else
               return new AuthResultDto
               {
                  Success = false,
                  Errors = new List<string>()
                  {
                     "Error while processing the reques5"
                  }
               };    
         }catch(Exception ex){
            Console.WriteLine("************************************");
            Console.WriteLine("I dont know");
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