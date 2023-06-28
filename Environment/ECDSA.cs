using Eduard;

namespace TrustGuard.Environment
{
	public class ECDSA
	{
		private BigInteger a;
		private BigInteger b;
		private System.Security.Cryptography.RandomNumberGenerator rand;

		private BigInteger p;
		private BigInteger N;

		private EllipticCurve curve;
		private BigInteger secretKey;

		private ECPoint basePoint;
		private ECPoint publicKey;

		public ECDSA(BigInteger a, BigInteger b, BigInteger p, BigInteger N, BigInteger secretKey, ECPoint basePoint)
		{
			this.a = a; this.b = b;
			this.p = p; this.N = N;

			this.secretKey = secretKey;
			this.basePoint = basePoint;

			curve = new EllipticCurve(a, b, p, N);
			this.publicKey = ECMath.Multiply(curve, secretKey, basePoint);
			rand = System.Security.Cryptography.RandomNumberGenerator.Create();
		}

		public byte[] Sign(byte[] buffer)
		{
			bool done = false;
			BigInteger r = 0, s = 0;

			do
			{
				BigInteger k = BigInteger.Next(rand, 1, N - 1);
				ECPoint Q = ECMath.Multiply(curve, k, basePoint);

				r = Q.GetAffineX() % N;
				if (r == 0) continue;

				BigInteger z = new BigInteger(buffer) % N;
				s = (k.Inverse(N) *(z + r * secretKey)) % N;
				if (s == 0) continue;
				done = true;
			}
			while (!done);

			byte[] sb = s.ToByteArray();
			byte[] rb = r.ToByteArray();
			byte[] result = new byte[64];

			for(int i = 0; i < 32; i++)
			{
				result[i + 32] = sb[i];
				result[i] = rb[i];
			}

			return result;
		}

		public bool Verify(byte[] hash, byte[] buffer)
		{
			if (publicKey == ECPoint.POINT_INFINITY) return false;
			if (ECMath.Multiply(curve, N, publicKey) != ECPoint.POINT_INFINITY) return false;
			byte[] temp = new byte[32];

			for (int i = 0; i < 32; i++) 
				temp[i] = buffer[i];

			BigInteger r = new BigInteger(temp);
			if (r < 1 || r > N - 1) return false;

			for (int i = 0; i < 32; i++) 
				temp[i] = buffer[i + 32];

			BigInteger s = new BigInteger(temp);
			if (s < 1 || s > N - 1) return false;

			BigInteger z = new BigInteger(hash) % N;
			BigInteger u1 = (z * s.Inverse(N)) % N;
			BigInteger u2 = (r * s.Inverse(N)) % N;

			ECPoint P = ECMath.Multiply(curve, u1, basePoint);
			ECPoint Q = ECMath.Multiply(curve, u2, publicKey);
			ECPoint R = ECMath.Add(curve, P, Q);

			if (R == ECPoint.POINT_INFINITY) return false;
			if (R.GetAffineX() % N != r) return false;
			return true;
		}
	}
}
