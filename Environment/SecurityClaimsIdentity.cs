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
	}
}
