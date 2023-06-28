namespace TrustGuard.Models
{
	public interface IJwtSettings
	{
		string Issuer { get; set; }
		string Audience { get; set; }
	}
}
