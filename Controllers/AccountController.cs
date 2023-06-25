using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrustGuard.Models;
using TrustGuard.Services;

#pragma warning disable

namespace TrustGuard.Controllers
{
    public class AccountController : Controller
    {
		readonly IAccountService accountService;

		public AccountController(IAccountService accountService)
		{ this.accountService = accountService; }

		public IActionResult Index()
        {
            return View();
        }

        public IActionResult Signup()
        {
            return View();
        }

		[HttpPost]
		public async Task<IActionResult> Signin(AccountRequestModel accountRequestModel)
		{
			AccountResponseModel accountResponseModel = await accountService.SignInAsync(accountRequestModel);

			if (accountResponseModel.status < 0) return Redirect("/Account/Signup");
			else if (accountResponseModel.status == 0) return Redirect("/Account/?FailCode=true");
			var claims = new Claim[]
			{
				new Claim("id", accountResponseModel.id.Value.ToString()),
				new Claim(ClaimTypes.Name, accountResponseModel.username),
				new Claim(ClaimTypes.Email, accountResponseModel.address.ToString())
			};

			var identity = new ClaimsIdentity(claims, "User Identity");
			var userPrincipal = new ClaimsPrincipal(new[] { identity });
			await HttpContext.SignInAsync(userPrincipal);
			return Redirect("/Home/Dashboard");
		}

		public async Task<IActionResult> Signout()
		{
			await HttpContext.SignOutAsync();
			return Redirect("/Account/");
		}
	}
}
