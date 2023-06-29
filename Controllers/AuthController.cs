using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using TrustGuard.Environment;
using TrustGuard.Models;
using TrustGuard.Services;
#pragma warning disable

namespace TrustGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        readonly IAccountService accountService;
        readonly IApplicationService applicationService;

        public AuthController(IAccountService accountService, IApplicationService applicationService)
        {
            this.accountService = accountService;
            this.applicationService = applicationService;
        }

        [HttpPost]
        public async Task<IActionResult> Auth([FromForm]AccountRequestModel accountRequestModel)
        {
            /* required for account authorization */
            AccountResponseModel res = await accountService.SignInAsync(accountRequestModel);
            if (Request.Headers.ContainsKey("ClientId") && Request.Headers.ContainsKey("ClientSecret"))
            {
                if (res.status == 1)
                {
                    string userId = res.id.Value.ToString();
                    string clientId = Request.Headers["ClientId"].ToString();
                    string clientSecret = Request.Headers["ClientSecret"].ToString();

                    /* save tokens to database, output them to user after */
                    TokenViewModel token = await applicationService.AuthenticateAsync(userId, clientId, clientSecret);
                    return Ok(token);
                }
                else
                    return Unauthorized();
            }
            else
                return NotFound();
        }

        [HttpPut("{Refresh}")]
        public async Task<IActionResult> Refresh([FromForm]TokenViewModel tokenViewModel)
        {
            if (Request.Headers.ContainsKey("ClientId") && Request.Headers.ContainsKey("ClientSecret"))
            {
                string clientId = Request.Headers["ClientId"].ToString();
                string clientSecret = Request.Headers["ClientSecret"].ToString();

                if (!string.IsNullOrEmpty(tokenViewModel.access_token) && !string.IsNullOrEmpty(tokenViewModel.refresh_token))
                {
                    /* revoke tokens */
                    TokenViewModel tokens = await applicationService
                        .RefreshTokenAsync(tokenViewModel.refresh_token,
                        tokenViewModel.access_token,
                        clientId,
                        clientSecret);

                    if (tokens != null)
                        return Ok(tokens);
                    else
                        return Unauthorized();
                }
                else
                    return Unauthorized();
            }
            else
                return NotFound();
        }

        [HttpPost("{Revoke}")]
        public async Task<IActionResult> Revoke([FromForm]TokenViewModel tokens)
        {
            // it is app identified?
            if (Request.Headers.ContainsKey("ClientId") && Request.Headers.ContainsKey("ClientSecret"))
            {
                string clientId = Request.Headers["ClientId"].ToString();
                string clientSecret = Request.Headers["ClientSecret"].ToString();

                if (!string.IsNullOrEmpty(tokens.access_token) && !string.IsNullOrEmpty(tokens.refresh_token))
                {
                    /* revoke tokens */
                    int status = await applicationService
                        .RevokeTokenAsync(tokens.refresh_token,
                        tokens.access_token,
                        clientId,
                        clientSecret);

                    if (status > 0)
                        return Ok();
                    else
                        return Unauthorized();
                }
                else
                    return Unauthorized();
            }
            else
                return NotFound();
        }
    }
}
