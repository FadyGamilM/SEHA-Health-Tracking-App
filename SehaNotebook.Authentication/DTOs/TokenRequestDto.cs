using System.ComponentModel.DataAnnotations;

namespace SehaNotebook.Authentication.DTOs
{
   public class TokenRequestDto
   {
      [Required]
      public string AccessToken {get; set;}
      [Required]
      public string RefreshToken{get; set;}
   }
}