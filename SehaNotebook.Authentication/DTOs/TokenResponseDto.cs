using System.ComponentModel.DataAnnotations;

namespace SehaNotebook.Authentication.DTOs
{
   public class TokenResponseDto
   {
      public string AccessToken {get; set;}
      public string RefreshToken{get; set;}
   }
}