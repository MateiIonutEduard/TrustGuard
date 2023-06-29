#pragma warning disable

using TrustGuard;
using TrustGuard.Environment;
using TrustGuard.Models;

namespace TrustGuard.Environment
{
    public class TokenViewModel
    {
        public string refresh_token { get; set; }
        public string access_token { get; set; }
    }
}
