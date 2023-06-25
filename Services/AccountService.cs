using TrustGuard.Data;
using TrustGuard.Models;
using Microsoft.EntityFrameworkCore;
#pragma warning disable

namespace TrustGuard.Services
{
	public class AccountService : IAccountService
	{
		readonly IAdminService adminService;
		readonly ICryptoService cryptoService;
		readonly TrustGuardContext guardContext;

		public AccountService(TrustGuardContext guardContext, IAdminService adminService, ICryptoService cryptoService)
		{
			this.adminService = adminService;
			this.cryptoService = cryptoService;
			this.guardContext = guardContext;
		}

		public async Task<AccountResponseModel> SignInAsync(AccountRequestModel accountRequestModel)
		{
			Account? account = await guardContext.Account
				.FirstOrDefaultAsync(e => e.Address == accountRequestModel.address);

			AccountResponseModel accountResponseModel = new AccountResponseModel();
			string encryptedPassword = cryptoService.EncryptPassword(accountRequestModel.password);

			if (account != null)
			{
				if (encryptedPassword.CompareTo(account.Password) != 0)
				{
					/* lost password */
					accountResponseModel.status = 0;
				}
				else
				{
					/* account is signed in successfully */
					accountResponseModel.status = 1;
					accountResponseModel.username = account.Username;
					accountResponseModel.address = account.Address;
					accountResponseModel.id = account.Id;
				}
			}
			else
			{
				/* account does not exists */
				accountResponseModel.status = -1;
			}

			return accountResponseModel;
		}
	}
}
