using Eduard;
using TrustGuard.Data;

namespace TrustGuard.Core
{
    public class CryptoUtil
    {
        public static ECPoint Multiply(EllipticCurve curve, BigInteger k, ECPoint basePoint)
        {
            ECPoint point = ECMath.Multiply(curve, k, basePoint);
            return point;
        }
    }
}
