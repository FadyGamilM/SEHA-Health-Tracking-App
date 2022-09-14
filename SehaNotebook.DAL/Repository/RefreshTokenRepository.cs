using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SehaNotebook.DAL.Data;
using SehaNotebook.DAL.IRepository;
using SehaNotebook.Domain.Entities;
namespace SehaNotebook.DAL.Repository
{
   public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
   {
      public RefreshTokenRepository(ILogger logger, AppDbContext context) : base(context, logger)
      {}
      //! concrete implementation of GetAll() method which is one of the general CRUD inherited from the GenericRepository
      public override async Task<IEnumerable<RefreshToken>> GetAll()
      {
         try{
            // utilize the _dbSet from the GenericRepository class
            var RefreshTokens = await _dbSet
                                          .Where(RefreshToken => RefreshToken.Status == true)
                                          .AsNoTracking()
                                          .ToListAsync();
            return RefreshTokens;
         }catch(Exception exeption){
            // utilize the _logger from the GenericRepository class
            _logger.LogError(exeption, $"'{typeof(RefreshTokenRepository)}' : [GetAll] API has generated an error");
            return new List<RefreshToken>();
         }
      }

      public async Task<RefreshToken> GetRefreshToken(string refreshToken)
      {
         try{
            var token = await _dbSet
                                          .Where(t => t.Token.ToLower() == refreshToken.ToLower())
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync();
            return token;
         }catch(Exception ex){
            // utilize the _logger from the GenericRepository class
            _logger.LogError(ex, $"'{typeof(RefreshTokenRepository)}' : [GetAll] API has generated an error");
            return new RefreshToken();
         }
      }

      public async Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken)
      {
         try
         {
            var token = await _dbSet
                                          .Where(t => t.Token.ToLower() == refreshToken.Token.ToLower())
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync();
            if(token == null)
               return false;
            else
               token.IsUsed = refreshToken.IsUsed;
            return true;
         }
         catch(Exception ex)
         {
            // utilize the _logger from the GenericRepository class
            _logger.LogError(ex, $"'{typeof(RefreshTokenRepository)}' : [MarkRefreshTokenAsUsed] API has generated an error");
            return false;
         }      
      }
   }
}