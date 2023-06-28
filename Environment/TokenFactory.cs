using Newtonsoft.Json;
using System.Text;

namespace TrustGuard.Environment
{
	public class TokenFactory
	{
		public TokenFactory()
		{ }

		/* require header payload to JWT tokens */
		public string GetHeader()
		{
			var header = new Dictionary<string, string>
			{
				{ "alg", "TG256" },
				{ "typ", "JWT" }
			};

			string content = JsonConvert.SerializeObject(header, Formatting.Indented);
			byte[] data = Encoding.ASCII.GetBytes(content);
			return Convert.ToBase64String(data);
		}
	}
}
