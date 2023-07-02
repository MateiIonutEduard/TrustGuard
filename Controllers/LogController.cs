using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustGuard.Services;
using TrustGuard.Models;

namespace TrustGuard.Controllers
{
    public class LogController : Controller
    {
        readonly LogService logService;

        public LogController(LogService logService)
        { this.logService = logService; }

        [HttpPost, Authorize]
        public async Task<IActionResult> Index(int? page, string clientId, int appId, string logId)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            // remove when user is authenticated
            if (!string.IsNullOrEmpty(userId))
            {
                await logService.RemoveLogAsync(logId);
                if (page != null && page.Value > 1)
                {
                    int results = (page.Value - 1) << 3;
                    List<Log> logs = await logService.GetLogsByApplicationAsync(clientId);
                    if(logs.Count > results) return Redirect($"/Home/Details/?id={appId}&page={page.Value}");
                    else return Redirect($"/Home/Details/?id={appId}");
                }
                else return Redirect($"/Home/Details/?id={appId}");
            }
            else
                return Redirect("/Account/");
        }
    }
}
