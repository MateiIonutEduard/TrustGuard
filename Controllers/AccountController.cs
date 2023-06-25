using Microsoft.AspNetCore.Mvc;

namespace TrustGuard.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
