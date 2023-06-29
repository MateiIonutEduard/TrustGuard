using Eduard;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using ECPoint = Eduard.ECPoint;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
#pragma warning disable

namespace TrustGuard.Environment
{
	public class TokenFactory
	{
		private EllipticCurve curve;
		private ECPoint basePoint;
		private RandomNumberGenerator rand;

		public TokenFactory(EllipticCurve curve, ECPoint basePoint)
		{
			this.curve = curve;
			this.basePoint = basePoint;
			rand = RandomNumberGenerator.Create();
		}

		private byte[] ComputeHash(byte[] buffer)
		{
			/* get SHA-256 hash as byte array */
			using (SHA256 sha256Hash = SHA256.Create())
			{
				byte[] bytes = sha256Hash.ComputeHash(buffer);
				return bytes;
			}
		}

		private int GetUnixTime(DateTime dateTime)
		{
			/* converts datetime to unix timestamp */
			int timestamp = (int)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			return timestamp;
		}

		public TokenModel SignToken(SecurityTokenDescription tokenDescription)
		{
			SecurityClaimsIdentity identity = tokenDescription.Subject;
			identity.AddClaim(ClaimType.Issuer, tokenDescription.Issuer);

			identity.AddClaim(ClaimType.Audience, tokenDescription.Audience);
			identity.AddClaim(ClaimType.Expires, GetUnixTime(DateTime.UtcNow.AddMinutes(5)).ToString());
			identity.AddClaim(ClaimType.Jti, Guid.NewGuid().ToString());

			string header = GetHeader();
			string payload = identity.GetPayload();

			string body = $"{header}.{payload}";
			byte[] buffer = Encoding.ASCII.GetBytes(body);

			BigInteger secretKey = BigInteger.Next(rand, 1, curve.order - 1);
			ECDSA ecdsa = new ECDSA(curve, secretKey, basePoint);

			/* sign hash */
			byte[] hash = ComputeHash(buffer);
			byte[] signedBody = ecdsa.Sign(hash);
			string signedBodyString = Convert.ToBase64String(signedBody);

			byte[] secretBytes = secretKey.ToByteArray();
			string secretEncoded = Convert.ToBase64String(secretBytes);

			byte[] array = new byte[33];
			ECPoint publicKey = ECMath.Multiply(curve, secretKey, basePoint);

			byte[] publicArray = publicKey.GetAffineX().ToByteArray();
			for (int i = 0; i < 32; i++) array[i] = publicArray[i];

			BigInteger N2 = curve.order >> 1;
			array[32] = (byte)(publicKey.GetAffineY() >= N2 ? 0x03 : 0x02);

			string refresh_token = Convert.ToBase64String(array);
			string access_token = $"{header}.{payload}.{signedBodyString}";

			/* signed token with public key and secret key */
			TokenModel tokenModel = new TokenModel
			{
				access_token = access_token,
				refresh_token = refresh_token,
				secretKey = secretEncoded
			};

			return tokenModel;
		}

		public int VerifyToken(string publicKey, string accessToken)
		{
			string[] parts = accessToken.Split(".");
			byte[] signature = Convert.FromBase64String(parts[2]);

			string body = $"{parts[0]}.{parts[1]}";
			byte[] buffer = Encoding.ASCII.GetBytes(body);
			byte[] hash = ComputeHash(buffer);

			byte[] payloadPart = Convert.FromBase64String(parts[1]);
			string content = Encoding.ASCII.GetString(payloadPart);

			Dictionary<string, string> keyValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
			int seconds = Convert.ToInt32(keyValues[ClaimType.Expires]);

			/* check if token expired */
			DateTime current = DateTime.UtcNow;
			DateTime signDate = new DateTime(1970, 1, 1).AddSeconds(seconds);
			if (current.CompareTo(signDate) == 1) return 0;

			byte[] data = Convert.FromBase64String(publicKey);
			byte[] xdata = new byte[32];

			for (int i = 0; i < 32; i++) xdata[i] = data[i];
			BigInteger x = new BigInteger(xdata);

			BigInteger N2 = curve.field >> 1;
			BigInteger y = EllipticCurve.Sqrt(curve.Eval(x), curve.field);

			if (y < N2 && data[32] == 0x03) y = curve.field - y;
			if (y >= N2 && data[32] == 0x02) y = curve.field - y;
			ECPoint publicPoint = new ECPoint(x, y);

			/* validate signature */
			ECDSA ecdsa = new ECDSA(curve, publicPoint, basePoint);
			int res = ecdsa.Verify(hash, signature) ? 1 : -1;
			return res;
		}

		/* require header payload to JWT tokens */
		private string GetHeader()
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
