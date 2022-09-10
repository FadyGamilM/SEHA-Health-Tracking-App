namespace SehaNotebook.Domain.DTOs
{
   public class CreateUserDto
   {
      public string FirstName {get; set;}
      public string LastName {get; set;}
      public string Email {get; set;}
      public string Phone {get; set;}
      public string DateOfBirth {get; set;}
      public string Country {get; set;}
   }
}