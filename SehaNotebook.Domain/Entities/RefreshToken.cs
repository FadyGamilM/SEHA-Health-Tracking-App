using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SehaNotebook.Domain.Entities
{
   public class RefreshToken : BaseEntity
   {
      // user id when he/she logged in
      public string UserId {get; set;}
      public string Token {get; set;}
      // the jti generated at the claims when the jwt id has been requested
      public string Jti {get; set;}
      // to make sure that this token has been used only once
      public bool IsUsed {get; set;}
      /**
      Make Sure that the refresh token is valid
      if you are trying to login using mobile device and browser at the same time
      so you will have 2 pairs of tokens for each device .. and this is invalid so we have to revoke it
      */
      public bool IsRevoked { get; set; }
      public DateTime ExpiryDate {get; set;}
      // connect this table with the User identity table
      [ForeignKey(nameof(UserId))]
      public IdentityUser User {get; set;}
   }
}