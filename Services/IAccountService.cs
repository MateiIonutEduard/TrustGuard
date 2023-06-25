using TrustGuard.Models;

namespace TrustGuard.Services
{
	public interface IAccountService
	{
		Task<string> GetAccountAvatarAsync(int id);
		Task<AccountResponseModel> SendWelcomeAsync(int id);
        Task<AccountResponseModel> SendWebcodeAsync(string address);
        Task<AccountResponseModel> SignInAsync(AccountRequestModel accountRequestModel);
		Task<AccountResponseModel> SignUpAsync(AccountRequestModel accountRequestModel);
	}
}
