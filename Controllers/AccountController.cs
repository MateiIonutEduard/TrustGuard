﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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

		[Authorize, HttpPost]
		public async Task<IActionResult> Remove()
		{
			string? userId = HttpContext.User?.Claims?
				.FirstOrDefault(u => u.Type == "id")?.Value;

			if (!string.IsNullOrEmpty(userId))
			{
				/* removes specified account */
				int UserId = Convert.ToInt32(userId);
				await accountService.RemoveAccountAsync(UserId);
				await HttpContext.SignOutAsync();
			}

			return Redirect("/Account/");
		}

		[Authorize]
        public async Task<IActionResult> Preferences()
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            int UserId = Convert.ToInt32(userId);
            Account account = await accountService.GetAccountAsync(UserId);

            bool failCode = HttpContext.Request.Query.ContainsKey("FailCode") ?
                Convert.ToBoolean(HttpContext.Request.Query["FailCode"]) : false;

            if (!failCode)
                HttpContext.Session.Clear();

            ViewData["state"] = account;
            return View("Views/Account/Preferences.cshtml", ViewData["state"]);
        }

		[HttpPost, Authorize]
		public async Task<IActionResult> Preferences(AccountRequestModel accountRequestModel)
		{
			string? userId = HttpContext.User?.Claims?
				.FirstOrDefault(u => u.Type == "id")?.Value;

			int UserId = Convert.ToInt32(userId);
			accountRequestModel.Id = UserId;
			AccountResponseModel accountResponseModel = await accountService.UpdateAccountPreferencesAsync(accountRequestModel);

			if (accountResponseModel.status < 0)
			{
				/* password does not match */
				HttpContext.Session.SetString("confirmPassword", accountRequestModel.confirmPassword);
				return Redirect("/Account/Preferences/?FailCode=true");
			}
			/* is logged off */
			else if (accountResponseModel.status == 0)
				return Redirect("/Account/");

			/* updated successful */
			await HttpContext.SignOutAsync();
			var claims = new Claim[]
{
				new Claim("id", accountResponseModel.id.Value.ToString()),
				new Claim(ClaimTypes.Name, accountResponseModel.username),
				new Claim(ClaimTypes.Email, accountResponseModel.address)
            };

			var identity = new ClaimsIdentity(claims, "User Identity");
			var userPrincipal = new ClaimsPrincipal(new[] { identity });
			await HttpContext.SignInAsync(userPrincipal);
			return Redirect("/Home/");
		}

		[HttpPost]
        public async Task<IActionResult> Send(string address)
        {
            AccountResponseModel accountResponseModel = await accountService.SendWebcodeAsync(address);
            if (accountResponseModel.status == -1) Redirect("/Account/Signup");
            return Redirect("/Account/Recover/?step=2");
        }

        [HttpPost]
        public async Task<IActionResult> Verify(string webcode)
        {
            AccountResponseModel accountResponseModel = await accountService.GetAccountByWebcodeAsync(webcode);
            if (accountResponseModel != null) return Redirect($"/Account/Recover/?step=3&uid={accountResponseModel.id}");
            else return Redirect("/Account/");
        }

        public async Task<IActionResult> Show(int id)
        {
            string filePath = await accountService.GetAccountAvatarAsync(id);
            int index = filePath.LastIndexOf(".");

            byte[] data = System.IO.File.ReadAllBytes(filePath);
            return File(data, $"image/{filePath.Substring(index + 1)}");
        }

        [HttpPost]
		public async Task<IActionResult> Signin([FromForm]AccountRequestModel accountRequestModel, [FromQuery]string? returnUrl)
		{
			AccountResponseModel accountResponseModel = await accountService.SignInAsync(accountRequestModel);
			HttpContext.Session.SetString("address", accountRequestModel.address);
			HttpContext.Session.SetString("password", accountRequestModel.password);

			/* if logged in successfully */
			if (accountResponseModel.status == 1)
			{
				HttpContext.Session.Remove("address");
				HttpContext.Session.Remove("password");
			}

			if (accountResponseModel.status < -1) return Redirect("/Account/?FailCode=-1");
			else if (accountResponseModel.status == -1) return Redirect("/Account/Signup");
			else if (accountResponseModel.status == 0) return Redirect("/Account/?FailCode=1");
			var claims = new Claim[]
			{
				new Claim("id", accountResponseModel.id.Value.ToString()),
				new Claim(ClaimTypes.Name, accountResponseModel.username),
				new Claim(ClaimTypes.Email, accountResponseModel.address.ToString())
			};

			var identity = new ClaimsIdentity(claims, "User Identity");
			var userPrincipal = new ClaimsPrincipal(new[] { identity });
			await HttpContext.SignInAsync(userPrincipal);

			if (!string.IsNullOrEmpty(returnUrl)) return Redirect($"/Home/Auth/?returnUrl={returnUrl}");
            else return Redirect("/Home/");
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

        [HttpPost]
        public async Task<IActionResult> UpdatePassword(AccountRequestModel accountRequestModel)
        {
            AccountResponseModel accountResponseModel = await accountService.UpdatePasswordAsync(accountRequestModel);

            if (accountResponseModel.status == 1)
            {
                var claims = new Claim[]
                {
                    new Claim("id", accountResponseModel.id.Value.ToString()),
                    new Claim(ClaimTypes.Name, accountResponseModel.username),
                    new Claim(ClaimTypes.Email, accountResponseModel.address)
                };

                var identity = new ClaimsIdentity(claims, "User Identity");
                var userPrincipal = new ClaimsPrincipal(new[] { identity });

                /* go back to home page */
                await HttpContext.SignInAsync(userPrincipal);
                return Redirect("/Home/");
            }
            else if (accountResponseModel.status == -1)
            {
                int uid = accountRequestModel.Id.Value;
                HttpContext.Session.SetString("password", accountRequestModel.password);

                HttpContext.Session.SetString("confirmPassword", accountRequestModel.confirmPassword);
                return Redirect($"/Account/Recover/?step=3&uid={uid}&error=true");
            }
            else
                /* unknown error, go to account login page */
                return Redirect("/Account/");
        }

        public async Task<IActionResult> Signout()
		{
			await HttpContext.SignOutAsync();
			return Redirect("/Account/");
		}
	}
}
