using Microsoft.AspNetCore.Mvc;
using SehaNotebook.DAL.IConfiguration;

namespace SehaNotebook.API.Controllers.V1
{
   [ApiController]
   [Route("api/v{version:apiVersion}/users")]
   [ApiVersion("1.0")]
   public class BaseController  : ControllerBase
   {
      protected IUnitOfWork _unitOfWork;
      public BaseController(IUnitOfWork unitOfWork)
      {
         _unitOfWork = unitOfWork;
      }
   }
}