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

        public async Task<string> GetAccountAvatarAsync(int id)
        {
            /* get user account */
            Account? account = await guardContext.Account
                .FirstOrDefaultAsync(u => u.Id == id);

			return account != null && account.Avatar != null ? account.Avatar
				: "./Storage/Account/avatar.png";
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

			/* require password encryption */
			string encryptedPassword = cryptoService
				.EncryptPassword(accountRequestModel.password);

			string avatarPath = null;

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
					Address = accountRequestModel.address
				};

				/* set account profile image, if does not null */
				if (!string.IsNullOrEmpty(avatarPath))
					account.Avatar = avatarPath;

				guardContext.Account.Add(account);
				await guardContext.SaveChangesAsync();

				/* new account was created */
				accountResponseModel.id = account.Id;
				accountResponseModel.username = account.Username;
				accountResponseModel.address = account.Address;
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

        public async Task<AccountResponseModel> SendWelcomeAsync(int id)
        {
            Account? account = await guardContext.Account
                .FirstOrDefaultAsync(e => e.Id == id);

            var accountResponseModel = new AccountResponseModel();

            if (account != null)
            {
                /* get recovery email template */
                string message = File.ReadAllText($"./TrustAdmin/welcome.txt");
                int index = message.IndexOf('{');
				string body = string.Empty;

                while (index >= 0)
				{
                    body = $"{message.Substring(0, index - 1)} {account.Username}{message.Substring(index + 4)}";
					index = body.IndexOf('{');
                }

                /* return status code */
                int res = adminService.SendEmail(account.Address, "TrustGuard Support", body);
                accountResponseModel.status = res < 1 ? 0 : 1;
            }
            else
            {
                // cannot find user account
                accountResponseModel.status = -1;
            }

            return accountResponseModel;
        }

        public async Task<AccountResponseModel> SendWebcodeAsync(string address)
        {
            Account? account = await guardContext.Account
                .FirstOrDefaultAsync(e => e.Address.CompareTo(address) == 0);

            var accountResponseModel = new AccountResponseModel();

            if (account != null)
            {
                string securityCode = Guid.NewGuid().ToString();
                account.SecurityCode = securityCode;
                await guardContext.SaveChangesAsync();

				/* get recovery email template */
				string message = File.ReadAllText($"./TrustAdmin/recover.txt");
				int index = message.IndexOf('{');

				/* create email body from template, after that get status code */
				string body = $"{message.Substring(0, index - 1)} {securityCode}{message.Substring(index + 4)}";
                int res = adminService.SendEmail(account.Address, "TrustGuard Support", body);
                accountResponseModel.status = res < 1 ? 0 : 1;
            }
            else
            {
                // something is wrong, cannot find user account
                accountResponseModel.status = -1;
            }

            return accountResponseModel;
        }
    }
}
