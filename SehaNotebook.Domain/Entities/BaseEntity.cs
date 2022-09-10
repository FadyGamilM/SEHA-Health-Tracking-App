using System;
namespace SehaNotebook.Domain.Entities
{
   public abstract class BaseEntity
   {
      public Guid Id { get; set; } = Guid.NewGuid();

      //! Soft Delete Approach ..
      public bool Status {get; set;} = true;

      public DateTime CreatedDate {get; set;} = DateTime.UtcNow;
      public DateTime UpdateDate {get; set;} = DateTime.UtcNow;

   }
}