#pragma warning disable

namespace TrustGuard.Models
{
	public class JwtSettings : IJwtSettings
	{
		public string Issuer { get; set; }
		public string Audience { get; set; }
	}
}
