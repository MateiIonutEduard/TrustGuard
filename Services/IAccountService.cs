using TrustGuard.Models;

namespace TrustGuard.Services
{
	public interface IAccountService
	{
		Task<string> GetAccountAvatarAsync(int id);
        Task<AccountResponseModel> SignInAsync(AccountRequestModel accountRequestModel);
		Task<AccountResponseModel> SignUpAsync(AccountRequestModel accountRequestModel);
	}
}
