#pragma warning disable

namespace TrustGuard.Data
{
    public class KeyPair
    {
        public int Id { get; set; }
        public string SecureKey { get; set; }
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }
        public bool ValidateLifetime { get; set; }

        public int BasePointId { get; set; }
        public BasePoint BasePoint { get; set; }
        public bool IsRevoked { get; set; }
    }
}
