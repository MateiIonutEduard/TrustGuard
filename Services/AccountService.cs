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

		public async Task<AccountResponseModel> SignUpAsync(AccountRequestModel accountRequestModel)
		{
			AccountResponseModel accountResponseModel = new AccountResponseModel();
			if (accountRequestModel.password.CompareTo(accountRequestModel.confirmPassword) != 0)
			{
				/* do not match */
				accountResponseModel.status = -1;
				return accountResponseModel;
			}
			string encryptedPassword = cryptoService.EncryptPassword(accountRequestModel.password);
			string avatarPath = "./Storage/Account/avatar.png";

			// check if username or password is taken
			Account account = await guardContext.Account
				.FirstOrDefaultAsync(e => e.Address == accountRequestModel.address || e.Username == accountRequestModel.username);

			if (account == null)
			{
				// copy avatar image first
				if (accountRequestModel.avatar != null)
				{
					avatarPath = $"./Storage/Account/{accountRequestModel.avatar.FileName}";
					MemoryStream ms = new MemoryStream();

					// save avatar logo at disk
					await accountRequestModel.avatar.CopyToAsync(ms);
					System.IO.File.WriteAllBytes(avatarPath, ms.ToArray());
				}

				account = new Account
				{
					Username = accountRequestModel.username,
					Password = cryptoService.EncryptPassword(accountRequestModel.password),
					Address = accountRequestModel.address,
					Avatar = avatarPath
				};

				guardContext.Account.Add(account);
				await guardContext.SaveChangesAsync();

				/* new account was created */
				accountResponseModel.id = account.Id;
				accountResponseModel.username = account.Username;
				accountResponseModel.status = 1;
			}
			else
			{
				/* user account is already registered */
				accountResponseModel.status = 0;
			}

			/* response result model */
			return accountResponseModel;
		}
	}
}
