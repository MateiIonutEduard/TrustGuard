using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace TrustGuard.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Signup()
        {
            return View();
        }

		public async Task<IActionResult> Signout()
		{
			await HttpContext.SignOutAsync();
			return Redirect("/Account/");
		}
	}
}
