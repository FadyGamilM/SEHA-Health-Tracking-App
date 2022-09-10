using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SehaNotebook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SehaNotebook.DAL.Data
{
   public class AppDbContext : IdentityDbContext 
   {
      public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
      {  }
      public virtual DbSet<User> Users {get; set;}
   }
}