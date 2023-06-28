using Newtonsoft.Json;
using System.Reflection.PortableExecutable;
using System.Text;

namespace TrustGuard.Environment
{
	public class SecurityClaimsIdentity
	{
		public Dictionary<string, string> claims;

		public SecurityClaimsIdentity()
		{ claims = new Dictionary<string, string>(); }

		public SecurityClaimsIdentity(params ClaimPair[] claimPairs)
		{
			claims = new Dictionary<string, string>();

			foreach (ClaimPair claimPair in claimPairs)
				claims.Add(claimPair.ClaimName, claimPair.ClaimValue);
		}

		public string GetPayload()
		{
			/* get JWT token payload, encoded in base64 string */
			string content = JsonConvert.SerializeObject(claims, Formatting.Indented);
			byte[] data = Encoding.ASCII.GetBytes(content);
			return Convert.ToBase64String(data);
		}
	}
}
