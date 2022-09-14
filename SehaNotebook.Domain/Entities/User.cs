using System;
namespace SehaNotebook.Domain.Entities
{
   public class User : BaseEntity
   {
      // id for the identity to add the user into the identity table
      public Guid IdentityId {get; set;}
      public string FirstName {get; set;}
      public string LastName {get; set;}
      public string Email {get; set;}
      public string Phone {get; set;}
      public DateTime DateOfBirth {get; set;}
      public string Country {get; set;}
      public string Sex {get; set;}
      public string Address {get; set;}
   }
}