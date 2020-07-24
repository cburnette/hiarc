using Hiarc.Api.REST.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Hiarc.Controllers
{
    [Route("")]
    public class RootController : HiarcControllerBase
    {
        public ActionResult Get()
        {
            return new OkResult();
        }
    }
}