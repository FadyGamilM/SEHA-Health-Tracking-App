using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SehaNotebook.DAL.IConfiguration;

namespace SehaNotebook.API.Controllers.V1
{
   [ApiController]
   [ApiVersion("1.0")]
   public class BaseController  : ControllerBase
   {
      protected IUnitOfWork _unitOfWork;
      protected UserManager<IdentityUser> _userManager;
      public BaseController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
      {
         _unitOfWork = unitOfWork;
         _userManager = userManager;
      }
   }
}