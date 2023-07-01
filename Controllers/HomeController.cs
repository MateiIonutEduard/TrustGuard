using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using TrustGuard.Data;
using TrustGuard.Environment;
using TrustGuard.Models;
using TrustGuard.Services;
#pragma warning disable

namespace TrustGuard.Controllers
{
    public class HomeController : Controller
    {
        readonly IAccountService accountService;
        readonly IApplicationService applicationService;
        readonly IHttpContextAccessor httpContextAccessor;

		public HomeController(IHttpContextAccessor httpContextAccessor, IAccountService accountService, IApplicationService applicationService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.applicationService = applicationService;
            this.accountService = accountService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult MyTrash()
        {
            return View();
        }

        public IActionResult Details()
        {
            return View();
        }

        public IActionResult RegisterApplication()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Me()
        {
            string header = HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(header))
            {
                // auth token
                string access_token = header.Split(' ')[1];
                var body = await applicationService.GetAccountByAppAsync(access_token);

                if (body != null)
                    return Ok(body);
                else
                    return NotFound();
            }
            else
                return Unauthorized();
        }

        public IActionResult Auth(string? returnUrl)
        {
            if (!Request.Cookies.ContainsKey("ClientId") || !Request.Cookies.ContainsKey("ClientSecret"))
                return BadRequest();

            /* get app credentials */
            string clientId = Request.Cookies["ClientId"];
            string clientSecret = Request.Cookies["ClientSecret"];

            /* decode callback url */
            byte[] decodedUrlBytes = Convert.FromBase64String(returnUrl);
            string decodedUrl = Encoding.ASCII.GetString(decodedUrlBytes);

            // save into cookies
            Response.Cookies.Append("ClientId", clientId);
			Response.Cookies.Append("ClientSecret", clientSecret);

            /* show page view */
            Response.Cookies.Append("Callback", decodedUrl);
			return View();
        }

        [HttpPost]
        public async Task<IActionResult> Auth()
        {
			/* get app credentials and callback url */
			string clientId = Request.Cookies["ClientId"];
			string clientSecret = Request.Cookies["ClientSecret"];
			string returnUrl = Request.Cookies["Callback"];

			/* user id */
			string? userId = HttpContext.User?.Claims?
				.FirstOrDefault(u => u.Type == "id")?.Value;

			/* save tokens to database, save them in cookies after */
			TokenViewModel token = await applicationService.AuthenticateAsync(userId, clientId, clientSecret);

			/* remove access token and refresh token if exists */
			if (Request.Cookies.ContainsKey("access_key")) Response.Cookies.Delete("access_token");
			if (Request.Cookies.ContainsKey("refresh_token")) Response.Cookies.Delete("refresh_token");

			if (token != null)
            {
				/* save tokens and go back */
				Response.Cookies.Append("access_token", token.access_token);
                Response.Cookies.Append("refresh_token", token.refresh_token);
                return Redirect(returnUrl);
            }
            else
                return NotFound();
		}

        [HttpPost]
        public async Task<IActionResult> Signin(AccountRequestModel accountRequestModel)
        {
			AccountResponseModel res = await accountService.SignInAsync(accountRequestModel);
			if (res.status < 0) return Redirect("/Account/Signup");
			else if (res.status == 0) return Redirect("/Account/?FailCode=true");

			if (res.status == 1)
			{
				string userId = res.id.Value.ToString();
				string clientId = Request.Cookies["ClientId"].ToString();

				string clientSecret = Request.Cookies["ClientSecret"].ToString();
				string returnUrl = Request.Cookies["Callback"];

				/* save tokens to database, output them to user after */
				TokenViewModel token = await applicationService.AuthenticateAsync(userId, clientId, clientSecret);

                /* remove access token and refresh token if exists */
                if (Request.Cookies.ContainsKey("access_key")) Response.Cookies.Delete("access_token");
				if (Request.Cookies.ContainsKey("refresh_token")) Response.Cookies.Delete("refresh_token");

				if (token != null)
				{
					/* save tokens and redirect */
					Response.Cookies.Append("access_token", token.access_token);
					Response.Cookies.Append("refresh_token", token.refresh_token);

					var claims = new Claim[]
                    {
				         new Claim("id", res.id.Value.ToString()),
				         new Claim(ClaimTypes.Name, res.username),
				         new Claim(ClaimTypes.Email, res.address.ToString())
                    };

					var identity = new ClaimsIdentity(claims, "User Identity");
					var userPrincipal = new ClaimsPrincipal(new[] { identity });

                    /* authenticate user before redirect */
					await HttpContext.SignInAsync(userPrincipal);
					return Redirect(returnUrl);
				}
				else
					return Unauthorized();
			}
			else
				return Unauthorized();
		}

        public async Task<IActionResult> Revoke()
        {
			/* get app credentials and callback url */
			string clientId = Request.Cookies["ClientId"];
			string clientSecret = Request.Cookies["ClientSecret"];
			string returnUrl = Request.Cookies["Callback"];

			// app identification
			if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(returnUrl))
			{
				string access_token = Request.Cookies["access_token"].ToString();
				string refresh_token = Request.Cookies["refresh_token"].ToString();

				if (!string.IsNullOrEmpty(access_token) && !string.IsNullOrEmpty(refresh_token))
				{
					/* revoke tokens */
					int status = await applicationService
						.RevokeTokenAsync(refresh_token,
						access_token,
						clientId,
						clientSecret);

                    Response.Cookies.Delete("access_token");
					Response.Cookies.Delete("refresh_token");
					Response.Cookies.Delete("Callback");

                    if (status > 0)
                        return Redirect(returnUrl);
                    else
                        return Unauthorized();
				}
				else
					return Unauthorized();
			}
			else
				return NotFound();
		}

        public async Task<IActionResult> Restore(int appId)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                int uid = Convert.ToInt32(userId);
                await applicationService.RestoreApplicationAsync(uid, appId);
                return Redirect("/Home/MyTrash");
            }
            else
                return Redirect("/Account/");
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> RemoveProject(int appId, bool? complete)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                int uid = Convert.ToInt32(userId);
                bool remove = complete != null ? complete.Value : false;
                await applicationService.RemoveApplicationAsync(remove, uid, appId);
                return Redirect("/Home/");
            }
            else
                return Redirect("/Account/");
        }

        public async Task<IActionResult> Show(int id)
        {
            Application? application = await applicationService.GetApplicationAsync(id);

            if (application != null && !string.IsNullOrEmpty(application.AppLogo))
            {
                int index = application.AppLogo.LastIndexOf(".");
                byte[] data = System.IO.File.ReadAllBytes(application.AppLogo);
                return File(data, $"image/{application.AppLogo.Substring(index + 1)}");
            }
            else
            if (application != null)
            {
                string defaultAppLogo = $"./Storage/Projects/defaultApp.png";
                int index = defaultAppLogo.LastIndexOf(".");
                byte[] data = System.IO.File.ReadAllBytes(defaultAppLogo);
                return File(data, $"image/{defaultAppLogo.Substring(index + 1)}");
            }
            else
                return NotFound();
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> CreateApp(ApplicationModel appModel)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                appModel.AccountId = Convert.ToInt32(userId);

                bool result = await applicationService
                    .CreateApplicationAsync(appModel);
                if (result) return Redirect($"/Home/");
                else
                {
                    ViewData["state"] = appModel;
                    return View($"Views/Home/RegisterApplication.cshtml", ViewData["state"]);
                }
            }
            else
                return Redirect("/Account/");
        }

        [HttpPost, Authorize]
        public IActionResult Search(bool? complete, AppQueryFilter appQueryFilter)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                int UserId = Convert.ToInt32(userId);
                ViewData["filter"] = appQueryFilter;
                if(complete != null && complete.Value) return View("Views/Home/MyTrash.cshtml", ViewData["filter"]);
                else return View("Views/Home/Index.cshtml", ViewData["filter"]);
            }
            else
                return Redirect("/Account/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}