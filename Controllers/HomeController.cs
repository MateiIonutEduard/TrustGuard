using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

        public IActionResult RegisterApplication()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
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
                    .CreateProductAsync(appModel);
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}