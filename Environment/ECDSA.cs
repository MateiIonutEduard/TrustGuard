using Eduard;

namespace TrustGuard.Environment
{
	public class ECDSA
	{
		private System.Security.Cryptography.RandomNumberGenerator rand;

		private EllipticCurve curve;
		private BigInteger secretKey;

		private ECPoint basePoint;
		private ECPoint publicKey;

		public ECDSA(EllipticCurve curve, BigInteger secretKey, ECPoint basePoint)
		{
			this.secretKey = secretKey;
			this.basePoint = basePoint;

			this.curve = curve;
			this.publicKey = ECMath.Multiply(curve, secretKey, basePoint);
			rand = System.Security.Cryptography.RandomNumberGenerator.Create();
		}

		public byte[] Sign(byte[] buffer)
		{
			bool done = false;
			BigInteger r = 0, s = 0;

			do
			{
				BigInteger k = BigInteger.Next(rand, 1, curve.order - 1);
				ECPoint Q = ECMath.Multiply(curve, k, basePoint);

				r = Q.GetAffineX() % curve.order;
				if (r == 0) continue;

				BigInteger z = new BigInteger(buffer) % curve.order;
				s = (k.Inverse(curve.order) *(z + r * secretKey)) % curve.order;
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
			if (ECMath.Multiply(curve, curve.order, publicKey) != ECPoint.POINT_INFINITY) return false;
			byte[] temp = new byte[32];

			for (int i = 0; i < 32; i++) 
				temp[i] = buffer[i];

			BigInteger r = new BigInteger(temp);
			if (r < 1 || r > curve.order - 1) return false;

			for (int i = 0; i < 32; i++) 
				temp[i] = buffer[i + 32];

			BigInteger s = new BigInteger(temp);
			if (s < 1 || s > curve.order - 1) return false;

			BigInteger z = new BigInteger(hash) % curve.order;
			BigInteger u1 = (z * s.Inverse(curve.order)) % curve.order;
			BigInteger u2 = (r * s.Inverse(curve.order)) % curve.order;

			ECPoint P = ECMath.Multiply(curve, u1, basePoint);
			ECPoint Q = ECMath.Multiply(curve, u2, publicKey);
			ECPoint R = ECMath.Add(curve, P, Q);

			if (R == ECPoint.POINT_INFINITY) return false;
			if (R.GetAffineX() % curve.order != r) return false;
			return true;
		}
	}
}
