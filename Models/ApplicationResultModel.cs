namespace TrustGuard.Models
{
    public class ApplicationResultModel
    {
        public int Pages { get; set; }
        public int Results { get; set; }
        public ApplicationViewModel[]? ApplicationViewModels { get; set; }
    }
}
