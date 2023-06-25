using TrustGuard.Models;

namespace TrustGuard.Services
{
	public interface IAccountService
	{
		Task<AccountResponseModel> SignInAsync(AccountRequestModel accountRequestModel);
		Task<AccountResponseModel> SignUpAsync(AccountRequestModel accountRequestModel);
	}
}
