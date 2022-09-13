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
      public JwtGenerator(IOptionsMonitor<JwtConfig> optionsMonitor, IUnitOfWork unitOfWork)
      {
         _jwtConfig = optionsMonitor.CurrentValue;
         _unitOfWork = unitOfWork;
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
         };

         await _unitOfWork.RefreshTokenRepository.Add(refreshToken);
         await _unitOfWork.CompleteAsyncOperations();

         return new TokenResponseDto{
            AccessToken = jwtToken,
            RefreshToken = refreshToken.Token
         };
      }
   }
}