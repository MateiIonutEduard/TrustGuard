using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustGuard.Services;

namespace TrustGuard.Controllers
{
    public class LogController : Controller
    {
        readonly LogService logService;

        public LogController(LogService logService)
        { this.logService = logService; }

        [HttpPost, Authorize]
        public async Task<IActionResult> Index(int appId, string logId)
        {
            string? userId = HttpContext.User?.Claims?
                .FirstOrDefault(u => u.Type == "id")?.Value;

            // remove when user is authenticated
            if (!string.IsNullOrEmpty(userId))
            {
                await logService.RemoveLogAsync(logId);
                return Redirect($"/Home/Details/?id={appId}");
            }
            else
                return Redirect("/Account/");
        }
    }
}
