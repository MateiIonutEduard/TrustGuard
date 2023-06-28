using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TrustGuard.Data;
using TrustGuard.Models;
using TrustGuard.Services;

namespace TrustGuard.Controllers
{
    public class HomeController : Controller
    {
        readonly IApplicationService applicationService;

        public HomeController(IApplicationService applicationService)
        {
            this.applicationService = applicationService;
        }

        public IActionResult Index()
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

        [HttpPost, Authorize]
        public async Task<IActionResult> RemoveProject(int appId)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                int uid = Convert.ToInt32(userId);
                await applicationService.RemoveApplicationAsync(uid, appId);
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
        public IActionResult Search(AppQueryFilter appQueryFilter)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                int UserId = Convert.ToInt32(userId);
                ViewData["filter"] = appQueryFilter;
                return View("Views/Home/Index.cshtml", ViewData["filter"]);
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