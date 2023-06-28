#pragma warning disable

namespace TrustGuard.Environment
{
	public class TokenModel
	{
		public string secretKey { get; set; }
		public string refresh_token { get; set; }
		public string access_token { get; set; }
	}
}
