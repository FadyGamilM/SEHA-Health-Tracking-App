using System.ComponentModel.DataAnnotations;

namespace SehaNotebook.Authentication.DTOs
{
   public class RegisterRequestDto
   {
      [Required]
      public string FirstName { get; set; }
      [Required]
      public string LastName { get; set; }
      [Required]
      public string Email { get; set; }
      [Required]
      public string Password { get; set; }
   }
}