using SehaNotebook.Domain.Entities;
namespace SehaNotebook.DAL.IRepository
{
   public interface IRefreshTokenRepository : IGenericRepository <RefreshToken> 
   {
      Task<RefreshToken> GetRefreshToken(string refreshToken);
      Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken);
   }
}