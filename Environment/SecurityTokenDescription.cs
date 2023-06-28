#pragma warning disable

namespace TrustGuard.Environment
{
	public class SecurityTokenDescription
	{
		public SecurityClaimsIdentity Subject { get; set; }
		public string Issuer { get; set; }
		public string Audience { get; set; }
		public DateTime Expires { get; set; }
	}
}
