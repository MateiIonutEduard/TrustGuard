using TrustGuard.Data;
using TrustGuard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

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

		public async Task<bool> RemoveAccountAsync(int userId)
		{
			Account? account = await guardContext.Account
				.FirstOrDefaultAsync(e => e.Id == userId);

			if (account != null)
			{
				Application[] apps = await guardContext.Application
					.Where(p => p.AccountId == userId)
					.ToArrayAsync();

                foreach (var app in apps)
                {
					/* remove app logo if is not default */
					if (!string.IsNullOrEmpty(app.AppLogo) && !app.AppLogo.EndsWith("defaultApp.png"))
						File.Delete(app.AppLogo);

					BasePoint[] basePoints = await guardContext.BasePoint
						.Where(p => p.ApplicationId == app.Id)
						.ToArrayAsync();

					/* remove all base points for each app */
					guardContext.BasePoint.RemoveRange(basePoints);
					await guardContext.SaveChangesAsync();
                }

				/* remove applications */
				guardContext.Application.RemoveRange(apps);
				await guardContext.SaveChangesAsync();

                /* only if avatar is not default image */
                if (!account.Avatar.EndsWith("avatar.png"))
					File.Delete(account.Avatar);

				/* remove account entity from database */
				guardContext.Account.Remove(account);
				await guardContext.SaveChangesAsync();
			}

			// not exists
			return false;
		}

		public async Task<AccountResponseModel> UpdateAccountPreferencesAsync(AccountRequestModel accountRequestModel)
		{
			Account? account = await guardContext.Account
				.FirstOrDefaultAsync(e => e.Id == accountRequestModel.Id.Value);

			AccountResponseModel accountResponseModel = new AccountResponseModel();
			if (accountRequestModel.password.CompareTo(accountRequestModel.confirmPassword) != 0)
			{
				/* passwords do not match */
				accountResponseModel.status = -1;
				return accountResponseModel;
			}

			/* update existing account */
			if (account != null)
			{
				string encryptedPassword = cryptoService.EncryptPassword(accountRequestModel.password);
				string avatarPath = "./Storage/Account/avatar.png";
				bool updateAvatar = false;

				/* copy avatar image first */
				if (accountRequestModel.avatar != null)
				{
					avatarPath = $"./Storage/Account/{accountRequestModel.avatar.FileName}";
					MemoryStream ms = new MemoryStream();

					/* save avatar logo, when hash 
                       have different values */
					await accountRequestModel.avatar.CopyToAsync(ms);
					byte[] oldData = await File.ReadAllBytesAsync(account.Avatar);

					string lhash = cryptoService.ComputeHash(oldData);
					string rhash = cryptoService.ComputeHash(ms.ToArray());

					if (lhash.CompareTo(rhash) != 0)
					{
						// remove if avatar is not default image
						if (!account.Avatar.EndsWith("avatar.png"))
							File.Delete(account.Avatar);

						// save image to file
						System.IO.File.WriteAllBytes(avatarPath, ms.ToArray());
						updateAvatar = true;
					}
				}

				account.Address = accountRequestModel.address;
				account.Username = accountRequestModel.username;

				account.Password = encryptedPassword;
				account.Address = accountRequestModel.address;

				/* update success */
				if (updateAvatar) account.Avatar = avatarPath;
				await guardContext.SaveChangesAsync();

				accountResponseModel.id = account.Id;
				accountResponseModel.username = account.Username;

				accountResponseModel.address = accountRequestModel.address;
				accountResponseModel.status = 1;
			}
			else
			{
				/* account does not exists, create new one */
				accountResponseModel.status = 0;
				return accountResponseModel;
			}

			return accountResponseModel;
		}

		public async Task<Account?> GetAccountAsync(int id)
        {
            /* get user account */
            Account? account = await guardContext.Account
                .FirstOrDefaultAsync(u => u.Id == id);

            account.Password = cryptoService.DecryptPassword(account.Password);
            return account;
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
                /* get welcome email template */
                string message = File.ReadAllText($"./TrustAdmin/welcome.txt");
                int index = message.IndexOf('{');
				string body = $"{message.Substring(0, index - 1)} {account.Username}{message.Substring(index + 4)}";

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

        public async Task<AccountResponseModel> UpdatePasswordAsync(AccountRequestModel accountRequestModel)
        {
			AccountResponseModel accountResponseModel = new AccountResponseModel();

            // passwords does not match
            if (accountRequestModel.password.CompareTo(accountRequestModel.confirmPassword) != 0)
                accountResponseModel.status = -1;
            else
            {
                Account? account = await guardContext.Account
                    .FirstOrDefaultAsync(e => e.Id == accountRequestModel.Id);

                if (account != null)
                {
                    // update account info
                    accountResponseModel.id = account.Id;
                    accountResponseModel.username = account.Username;
                    accountResponseModel.address = account.Address;

                    // update password successfully
                    account.Password = cryptoService.EncryptPassword(accountRequestModel.password);
                    await guardContext.SaveChangesAsync();
                    accountResponseModel.status = 1;
                }
                else
                    /* account not found */
                    accountResponseModel.status = 0;
            }

            // returns response model to maintains the logic
            return accountResponseModel;
        }

        public async Task<AccountResponseModel> GetAccountByWebcodeAsync(string securityCode)
        {
            Account? account = await guardContext.Account
                .FirstOrDefaultAsync(e => e.SecurityCode.CompareTo(securityCode) == 0);

            if (account != null)
            {
                AccountResponseModel accountResponseModel = new AccountResponseModel();
                accountResponseModel.username = account.Username;

                accountResponseModel.id = account.Id;
                accountResponseModel.status = 1;

                accountResponseModel.address = account.Address;
                return accountResponseModel;
            }

            return null;
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

				string body = message;
				int k = 0;

                while (index >= 0)
                {
					string str = k == 0 ? $"{account.Username}" : $">{securityCode}";
                    body = $"{body.Substring(0, index - 1)} {str}{body.Substring(index + 4)}";
                    index = body.IndexOf('{');
					k++;
                }

                /* create email body from template, after that get status code */
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
