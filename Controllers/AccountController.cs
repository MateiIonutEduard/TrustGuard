using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrustGuard.Data;
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

        public IActionResult Recover()
        {
            return View();
        }

        public async Task<IActionResult> Show(int id)
        {
            string filePath = await accountService.GetAccountAvatarAsync(id);
            int index = filePath.LastIndexOf(".");

            byte[] data = System.IO.File.ReadAllBytes(filePath);
            return File(data, $"image/{filePath.Substring(index + 1)}");
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
			return Redirect("/Home/");
		}

		[HttpPost]
		public async Task<IActionResult> Register(AccountRequestModel accountRequestModel)
		{
			AccountResponseModel accountResponseModel = await accountService.SignUpAsync(accountRequestModel);
			if (accountResponseModel.status == -1)
			{
				ViewData["state"] = accountRequestModel;
				return View("Views/Account/Signup.cshtml", ViewData["state"]);
			}

			if (accountResponseModel.status <= 0) return Redirect("/Account/Signup/?FailCode=0");

			var claims = new Claim[]
			{
				new Claim("id", accountResponseModel.id.Value.ToString()),
				new Claim(ClaimTypes.Name, accountResponseModel.username),
				new Claim(ClaimTypes.Email, accountResponseModel.address.ToString())
			};

			/* send welcome email to new user, and authenticate him */
			await accountService.SendWelcomeAsync(accountResponseModel.id.Value);
			var identity = new ClaimsIdentity(claims, "User Identity");

			var userPrincipal = new ClaimsPrincipal(new[] { identity });
			await HttpContext.SignInAsync(userPrincipal);
			return Redirect("/Home/");
		}

		public async Task<IActionResult> Signout()
		{
			await HttpContext.SignOutAsync();
			return Redirect("/Account/");
		}
	}
}
