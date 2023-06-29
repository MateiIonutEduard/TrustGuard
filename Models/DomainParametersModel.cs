using Eduard;
#pragma warning disable

namespace TrustGuard.Models
{
    public class DomainParametersModel
    {
        public BigInteger a { get; set; }
        public BigInteger b { get; set; }
        public BigInteger p { get; set; }
        public BigInteger N { get; set; }
        public ECPoint basePoint { get; set; }
    }
}
