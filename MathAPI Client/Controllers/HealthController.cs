using Microsoft.AspNetCore.Mvc;

namespace MathAPIClient.Controllers
{
    public class HealthController : Controller
    {
        [HttpGet("/health")]
        public IActionResult Index()
        {
            return Content("ok");
        }
    }
}