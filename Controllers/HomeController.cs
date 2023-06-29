using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

        public HomeController(IAccountService accountService, IApplicationService applicationService)
        {
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
        public async Task<IActionResult> Auth(AccountRequestModel accountRequestModel)
        {
            AccountResponseModel res = await accountService.SignInAsync(accountRequestModel);
            if (Request.Headers.ContainsKey("ClientId") && Request.Headers.ContainsKey("ClientSecret"))
            {
                if (res.status == 1)
                {
                    string userId = res.id.Value.ToString();
                    string clientId = Request.Headers["ClientId"].ToString();

                    string clientSecret = Request.Headers["ClientSecret"].ToString();
                    TokenViewModel token = await applicationService.AuthenticateAsync(userId, clientId, clientSecret);
                    return Ok(token);
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