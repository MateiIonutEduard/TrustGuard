﻿using Eduard;
using TrustGuard.Data;
using TrustGuard.Environment;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TrustGuard.Models;
using TrustGuard.Core;
using ECPoint = Eduard.ECPoint;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;
#pragma warning disable

namespace TrustGuard.Services
{
    public class ApplicationService : IApplicationService
    {
        readonly IAppSettings appSettings;
        readonly IJwtSettings jwtSettings;

        readonly IAdminService adminService;
        readonly LogService logService;

        readonly TrustGuardContext guardContext;
        readonly RandomNumberGenerator rand;

        public ApplicationService(LogService logService, IAdminService adminService, IAppSettings appSettings, IJwtSettings jwtSettings, TrustGuardContext guardContext)
        { 
            this.guardContext = guardContext;
            rand = RandomNumberGenerator.Create();
            this.adminService = adminService;

            this.logService = logService;
            this.appSettings = appSettings;
            this.jwtSettings = jwtSettings;
        }

        public async Task<int> ValidateLifetime(string access_token)
        {
            DomainParametersModel dpm = (
            from d in await guardContext.Domain.ToListAsync()
            join a in await guardContext.Application.ToListAsync() on d.Id equals a.DomainId
            join b in await guardContext.BasePoint.ToListAsync() on a.Id equals b.ApplicationId
            join k in await guardContext.KeyPair.ToListAsync() on b.Id equals k.BasePointId
            where k.AccessToken.CompareTo(access_token) == 0 && (a.IsDeleted == null || (a.IsDeleted != null && !a.IsDeleted.Value)) && (b.IsDeleted == null || (b.IsDeleted != null && !b.IsDeleted.Value)) && !k.IsRevoked
            select new DomainParametersModel
            {
                a = new BigInteger(d.a),
                b = new BigInteger(d.b),
                p = new BigInteger(d.p),
                N = new BigInteger(d.N),
                basePoint = new ECPoint(new BigInteger(b.x), new BigInteger(b.y))
            }).FirstOrDefault();

            KeyPair keyPair = await guardContext.KeyPair
                .FirstOrDefaultAsync(k => k.AccessToken.CompareTo(access_token) == 0 && !k.IsRevoked);

            if(keyPair != null)
            {
                EllipticCurve curve = new EllipticCurve(dpm.a, dpm.b, dpm.p, dpm.N);
                TokenFactory tokenFactory = new TokenFactory(curve, dpm.basePoint);

                int result = tokenFactory.VerifyToken(keyPair.RefreshToken, access_token, keyPair.ValidateLifetime);
                return result;
            }

            return -2;
        }

        public async Task<AccountBodyModel?> GetAccountByAppAsync(string accessToken)
        {
            // find key pair by access token
            KeyPair keyPair = await guardContext.KeyPair
                .FirstOrDefaultAsync(e => e.AccessToken.CompareTo(accessToken) == 0 && !e.IsRevoked);

            if(keyPair != null)
            {
                // get key pair owner
                Account? account = await guardContext.Account
                    .FirstOrDefaultAsync(e => e.Id == keyPair.AccountId);

                var body = new AccountBodyModel
                {
                    id = account.Id,
                    username = account.Username,
                    address = account.Address,
                    profile = $"{jwtSettings.Issuer}Account/Show/?id={account.Id}"
                };

                return body;
            }

            return null;
        }

        public async Task<Application?> GetApplicationByIdAsync(string? clientId, string? clientSecret)
        {
            Application? app = await guardContext.Application
                .FirstOrDefaultAsync(e => e.ClientId.CompareTo(clientId) == 0 && e.ClientSecret.CompareTo(clientSecret) == 0);

            /* get app by id */
            return app;
		}

        public async Task<int> RevokeTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret, bool validateLifetime = false)
        {
            DomainParametersModel dpm = (
                from d in await guardContext.Domain.ToListAsync()
                join a in await guardContext.Application.ToListAsync() on d.Id equals a.DomainId
                join b in await guardContext.BasePoint.ToListAsync() on a.Id equals b.ApplicationId
                join k in await guardContext.KeyPair.ToListAsync() on b.Id equals k.BasePointId
                where k.RefreshToken.CompareTo(refreshToken) == 0 && a.ClientId.CompareTo(clientId) == 0 && (a.IsDeleted == null || (a.IsDeleted != null && !a.IsDeleted.Value)) && (b.IsDeleted == null || (b.IsDeleted != null && !b.IsDeleted.Value)) && !k.IsRevoked
                select new DomainParametersModel
                {
                    a = new BigInteger(d.a),
                    b = new BigInteger(d.b),
                    p = new BigInteger(d.p),
                    N = new BigInteger(d.N),
                    basePoint = new ECPoint(new BigInteger(b.x), new BigInteger(b.y))
                }).FirstOrDefault();

            if(dpm != null)
            {
                /* revoke key pair */
                KeyPair keyPair = await guardContext.KeyPair
                    .FirstOrDefaultAsync(e => e.RefreshToken.CompareTo(refreshToken) == 0 && !e.IsRevoked);

                keyPair.IsRevoked = true;
                await guardContext.SaveChangesAsync();

                EllipticCurve curve = new EllipticCurve(dpm.a, dpm.b, dpm.p, dpm.N);
                TokenFactory tokenFactory = new TokenFactory(curve, dpm.basePoint);

                int result = tokenFactory.VerifyToken(refreshToken, accessToken, validateLifetime);
                string message = string.Empty;

                /* needs account username */
                Account account = await guardContext.Account
                    .FirstOrDefaultAsync(e => e.Id == keyPair.AccountId);

                if (result < 1)
                {
                    if(result == 0)
                    {
                        /* possibly suspicious */
                        message = $"User {account.Username} requests authorization revocation, with outdated access token.";
                        await logService.CreateLogAsync(clientId, message, Models.LogLevel.Warning);
                    }
                    else
                    {
                        /* be careful, this is dangerous */
                        message = "Anonymous user try to send invalid signed token.";
                        await logService.CreateLogAsync(clientId, message, Models.LogLevel.Danger);
                    }
                }

                message = $"User {account.Username} has revoked access token successfully.";
                await logService.CreateLogAsync(clientId, message, Models.LogLevel.Info);
                return result;
            }

            /* not exists */
            return -2;
        }

        public async Task<TokenViewModel?> RefreshTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret, bool validateLifetime = false)
        {
            DomainParametersModel dpm = (
                from d in await guardContext.Domain.ToListAsync()
                join a in await guardContext.Application.ToListAsync() on d.Id equals a.DomainId
                join b in await guardContext.BasePoint.ToListAsync() on a.Id equals b.ApplicationId
                join k in await guardContext.KeyPair.ToListAsync() on b.Id equals k.BasePointId
                where k.RefreshToken.CompareTo(refreshToken) == 0 && a.ClientId.CompareTo(clientId) == 0 && (a.IsDeleted == null || (a.IsDeleted != null && !a.IsDeleted.Value)) && (b.IsDeleted == null || (b.IsDeleted != null && !b.IsDeleted.Value)) && !k.IsRevoked
                select new DomainParametersModel
                {
                    a = new BigInteger(d.a),
                    b = new BigInteger(d.b),
                    p = new BigInteger(d.p),
                    N = new BigInteger(d.N),
                    basePoint = new ECPoint(new BigInteger(b.x), new BigInteger(b.y))
                }).FirstOrDefault();

            if (dpm != null)
            {
                /* revoke old key pair */
                KeyPair keyPair = await guardContext.KeyPair
                    .FirstOrDefaultAsync(e => e.RefreshToken.CompareTo(refreshToken) == 0 && !e.IsRevoked);

                // get account
                Account account = await guardContext.Account
                    .FirstOrDefaultAsync(e => e.Id == keyPair.AccountId);

                keyPair.IsRevoked = true;
                await guardContext.SaveChangesAsync();

                EllipticCurve curve = new EllipticCurve(dpm.a, dpm.b, dpm.p, dpm.N);
                TokenFactory tokenFactory = new TokenFactory(curve, dpm.basePoint);

                int result = tokenFactory.VerifyToken(refreshToken, accessToken, validateLifetime);
                string message = string.Empty;

                /* if access token not expired */
                if (result == 1)
                {
                    var desc = new SecurityTokenDescription
                    {
                        Issuer = jwtSettings.Issuer,
                        Audience = jwtSettings.Audience,
                        Subject = new SecurityClaimsIdentity()
                    };

                    desc.Subject.AddClaim("id", account.Id.ToString());
                    desc.Subject.AddClaim(ClaimType.Subject, account.Username);
                    desc.Subject.AddClaim(ClaimType.Email, account.Address);

                    var tokenModel = tokenFactory.SignToken(desc, validateLifetime);
                    KeyPair newKeyPair = new KeyPair
                    {
                        SecureKey = tokenModel.secretKey,
                        RefreshToken = tokenModel.refresh_token,
                        AccessToken = tokenModel.access_token,
                        BasePointId = keyPair.BasePointId,
                        ValidateLifetime = validateLifetime,
                        AccountId = account.Id,
                        IsRevoked = false
                    };

                    /* save keypair to database */
                    guardContext.KeyPair.Add(newKeyPair);
                    await guardContext.SaveChangesAsync();

                    TokenViewModel token = new TokenViewModel
                    {
                        access_token = tokenModel.access_token,
                        refresh_token = tokenModel.refresh_token
                    };

                    /* create log, that everything work successful */
                    string logBody = $"User {account.Username} successfully exchange token pair that needed at authorization.";
                    await logService.CreateLogAsync(clientId, logBody, Models.LogLevel.Info);

                    return token;
                }
                else
                if(result == 0)
                {
                    /* suspicious token pair exchange, with outdated access token */
                    message = $"User {account.Username} sent outdated access token.";
                    await logService.CreateLogAsync(clientId, message, Models.LogLevel.Warning);
                }
                else
                {
                    /* dangerous token pair exchange */
                    message = "Anonymous user try to exchange token pair by using invalid signed token.";
                    await logService.CreateLogAsync(clientId, message, Models.LogLevel.Danger);
                }
            }

            return null;
        }

        public async Task<TokenViewModel?> AuthenticateAsync(string? userId, string? clientId, string? clientSecret, bool validateLifetime = false)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                int uid = Convert.ToInt32(userId);
                Account? account = await guardContext.Account.FirstOrDefaultAsync(e => e.Id == uid);

                if (account != null)
                {
                    var desc = new SecurityTokenDescription
                    {
                        Issuer = jwtSettings.Issuer,
                        Audience = jwtSettings.Audience,
                        Subject = new SecurityClaimsIdentity()
                    };

                    desc.Subject.AddClaim("id", userId);
                    desc.Subject.AddClaim(ClaimType.Subject, account.Username);

                    desc.Subject.AddClaim(ClaimType.Email, account.Address);
                    TokenModel tokenModel = new TokenModel();

                    Application? application = await guardContext.Application
                        .FirstOrDefaultAsync(e => e.ClientId.CompareTo(clientId) == 0 && e.ClientSecret.CompareTo(clientSecret) == 0 && (e.IsDeleted == null || (e.IsDeleted != null && !e.IsDeleted.Value)));

                    if (application != null)
                    {
                        BasePoint basePoint = await guardContext.BasePoint
                            .FirstOrDefaultAsync(e => e.ApplicationId == application.Id && (e.IsDeleted == null || (e.IsDeleted != null && !e.IsDeleted.Value)));

                        Domain domain = await guardContext.Domain
                            .FirstOrDefaultAsync(e => e.Id == application.DomainId);

                        BigInteger a = new BigInteger(domain.a.ToString());
                        BigInteger b = new BigInteger(domain.b.ToString());

                        BigInteger p = new BigInteger(domain.p.ToString());
                        BigInteger N = new BigInteger(domain.N.ToString());
                        EllipticCurve curve = new EllipticCurve(a, b, p, N);

                        BigInteger x = new BigInteger(basePoint.x.ToString());
                        BigInteger y = new BigInteger(basePoint.y.ToString());

                        ECPoint G = new ECPoint(x, y);
                        TokenFactory tokenFactory = new TokenFactory(curve, G);

                        tokenModel = tokenFactory.SignToken(desc, validateLifetime);
                        KeyPair keyPair = new KeyPair
                        {
                            SecureKey = tokenModel.secretKey,
                            RefreshToken = tokenModel.refresh_token,
                            AccessToken = tokenModel.access_token,
                            ValidateLifetime = validateLifetime,
                            BasePointId = basePoint.Id,
                            AccountId = account.Id,
                            IsRevoked = false
                        };

                        /* save keypair to database */
                        guardContext.KeyPair.Add(keyPair);
                        await guardContext.SaveChangesAsync();

                    #if ENABLE_USER_WARN
                        /* get identification email template */
                        string message = File.ReadAllText($"./TrustAdmin/AreYou.txt");
                        int index = message.IndexOf('{');

                        string body = message;
                        int k = 0;

                        while (index >= 0)
                        {
                            string str = k == 0 ? $"{account.Username}" : $"\'{jwtSettings.Issuer}Home/Report";
                            body = $"{body.Substring(0, index - 1)} {str}{body.Substring(index + 4)}";
                            index = body.IndexOf('{');
                            k++;
                        }

                        /* create email body from template, after that get status code */
                        adminService.SendEmail(account.Address, "TrustGuard Support", body);
                    #endif

                        /* create log, that everything work successful */
                        string logBody = $"User {account.Username} successfully signed in to application.";
                        await logService.CreateLogAsync(clientId, logBody, Models.LogLevel.Info);

                        TokenViewModel token = new TokenViewModel
                        {
                            access_token = tokenModel.access_token,
                            refresh_token = tokenModel.refresh_token
                        };

                        return token;
                    }

                    /* error status code */
                    string logMessage = $"The application {clientId} was not found.";
                    await logService.CreateLogAsync(clientId, logMessage, Models.LogLevel.Error);
                }
            }

            string msg = "Unknown user request anonymus authentication at this app.";
            await logService.CreateLogAsync(clientId, msg, Models.LogLevel.Warning);
            return null;
        }

        public async Task<Application?> GetApplicationAsync(int id)
        {
            Application? application = await guardContext.Application
                .FirstOrDefaultAsync(p => p.Id == id);

            return application;
        }

        public async Task<ApplicationDetailsModel> GetApplicationDetailsAsync(int? id, int? page)
        {
            if (id == null) return null;
            int index = (page != null && page.Value >= 1) ? page.Value - 1 : 0;

            Application? app = await guardContext.Application
                .FirstOrDefaultAsync(p => p.Id == id);

            if(app != null)
            {
                /* main app details */
                ApplicationDetailsModel details = new ApplicationDetailsModel
                {
                    Id = app.Id,
                    AppName = app.AppName,
                    ClientId = app.ClientId,
                    AccountId = app.AccountId,
                    Description = app.Description,
                    ClientSecret = app.ClientSecret,
                    ModifiedAt = (app.ModifiedAt != null ? app.ModifiedAt.Value : app.CreatedAt),
                    IsDeleted = (app.IsDeleted != null ? app.IsDeleted.Value : false),
                    AppType = app.AppType,
                    ConnectedUsers = 0
                };

                /* unicity of user accounts */
                var map = new HashSet<int>();

                /* for each base point from elliptic curve */
                List<BasePoint> points = await guardContext.BasePoint
                    .Where(p => p.ApplicationId == app.Id)
                    .ToListAsync();

                foreach (BasePoint point in points)
                {
                    /* for all key pairs */
                    List<KeyPair> keyPairs = await guardContext.KeyPair
                        .Where(k => k.BasePointId == point.Id)
                        .ToListAsync();

                    foreach(KeyPair keyPair in keyPairs)
                    {
                        /* add unique user account */
                        if(!map.Contains(keyPair.AccountId))
                            map.Add(keyPair.AccountId);
                    }
                }

                List<Log> logs = await logService.GetLogsByApplicationAsync(app.ClientId);
                int counter = logs.Count;
                int totalPages = counter >> 3;

                if ((counter & 0x7) != 0)
                    totalPages++;

                details.Results = counter;
                details.TotalPages = totalPages;

                /* paginate results */
                details.Logs = logs.Skip(8 * index)
                    .Take(8).ToArray();

                /* now, return app details model */
                details.ConnectedUsers = map.Count;
                return details;
            }

            // not found
            return null;
        }

        public async Task<ApplicationResultModel> GetAppsByFilterAsync(bool complete, AppQueryFilter filter, string? userId, int? page)
        {
            int? uid = !string.IsNullOrEmpty(userId) ? Convert.ToInt32(userId) : null;
            int index = (page != null && page.Value >= 1) ? page.Value - 1 : 0;

            if (uid != null)
            {
                List<Application> apps = new List<Application>();
                List<Application> projects = new List<Application>();

                if (filter.ActivityType == 1)
                {
                    if (!complete)
                    {
                        apps = await guardContext.Application
                            .Where(p => p.AccountId == uid.Value && (p.IsDeleted != null ? !p.IsDeleted.Value : false))
                            .OrderByDescending(p => p.CreatedAt)
                            .ToListAsync();
                    }
                    else
                    {
                        apps = await guardContext.Application
                            .Where(p => p.AccountId == uid.Value && (p.IsDeleted != null && p.IsDeleted.Value))
                            .OrderByDescending(p => p.CreatedAt)
                            .ToListAsync();
                    }
                }
                else
                {
                    if (!complete)
                    {
                        apps = await guardContext.Application
                            .Where(p => p.AccountId == uid.Value && (p.IsDeleted != null ? !p.IsDeleted.Value : false))
                            .OrderByDescending(p => p.ModifiedAt != null ? p.ModifiedAt.Value : p.CreatedAt)
                            .ToListAsync();
                    }
                    else
                    {
                        apps = await guardContext.Application
                            .Where(p => p.AccountId == uid.Value && (p.IsDeleted != null && p.IsDeleted.Value))
                            .OrderByDescending(p => p.ModifiedAt != null ? p.ModifiedAt.Value : p.CreatedAt)
                            .ToListAsync();
                    }
                }

                bool[] marked = new bool[apps.Count];
                for (int t = 0; t < marked.Length; t++) marked[t] = false;

                /* apply query by app name */
                if(!string.IsNullOrEmpty(filter.AppName))
                {
                    for(int i = 0; i < apps.Count; i++)
                    {
                        if (!apps[i].AppName.ToLower().StartsWith(filter.AppName.ToLower()))
                            marked[i] = true;
                    }
                }

                /* query apps by using app type */
                for(int j = 0; j < apps.Count; j++)
                {
                    if (!marked[j] && filter.AppType != apps[j].AppType)
                        marked[j] = true;
                }

                // remove marked apps
                for(int k = 0; k < apps.Count; k++)
                {
                    if (!marked[k])
                        projects.Add(apps[k]);
                }

                int counter = projects.Count;
                int totalPages = counter >> 3;

                if ((counter & 0x7) != 0)
                    totalPages++;

                List<ApplicationViewModel> list = projects.Skip(8 * index).Take(8).ToList()
                    .Select(a => new ApplicationViewModel
                    {
                        Id = a.Id,
                        AppName = a.AppName,
                        ClientId = a.ClientId,
                        CreatedAt = a.CreatedAt,
                        ModifiedAt = (a.ModifiedAt != null ? a.ModifiedAt.Value : a.CreatedAt),
                        AppType = a.AppType
                    })
                    .ToList();

                ApplicationResultModel result = new ApplicationResultModel
                {
                    Pages = totalPages,
                    ApplicationViewModels = list.ToArray(),
                    Results = projects.Count
                };

                return result;
            }

            return null;
        }

        public async Task<bool?> RestoreApplicationAsync(int userId, int appId)
        {
            Application? app = await guardContext.Application
                .FirstOrDefaultAsync(p => p.Id == appId && (p.IsDeleted != null && p.IsDeleted.Value));

            if(app != null)
            {
                /* enable application back */
                if(app.AccountId == userId)
                {
                    app.IsDeleted = false;
                    await guardContext.SaveChangesAsync();
                }

                /* no rights */
                return false;
            }

            /* app has been removed completely */
            return null;
        }

        public async Task<bool?> RemoveApplicationAsync(bool complete, int userId, int appId)
        {
            /* temporary removing the application */
            if (!complete)
            {
                Application? project = await guardContext.Application
                    .FirstOrDefaultAsync(p => p.Id == appId && (p.IsDeleted != null ? !p.IsDeleted.Value : false));

                if (project != null)
                {
                    if (project.AccountId == userId)
                    {
                        /* disable project only */
                        project.IsDeleted = true;
                        await guardContext.SaveChangesAsync();
                    }

                    /* no rights */
                    return false;
                }
            }
            else
            {
                Application? project = await guardContext.Application
                    .FirstOrDefaultAsync(p => p.Id == appId && (p.IsDeleted != null && p.IsDeleted.Value));

                if (project != null)
                {
                    if (project.AccountId == userId)
                    {
                        /* remove app logo if is not default */
                        if (!string.IsNullOrEmpty(project.AppLogo) && !project.AppLogo.EndsWith("defaultApp.png"))
                            File.Delete(project.AppLogo);

                        BasePoint[] points = await guardContext.BasePoint
                            .Where(e => e.ApplicationId == project.Id)
                            .ToArrayAsync();

                        foreach(BasePoint point in points)
                        {
                            /* remove all key pairs that are generated 
                             * using each base point in the list */
                            List<KeyPair> keyPairs = await guardContext.KeyPair
                                .Where(k => k.BasePointId == point.Id)
                                .ToListAsync();

                            /* if base point list is not null and his items counter is greater than 0 */
                            if (keyPairs != null && keyPairs.Count > 0)
                            {
                                guardContext.KeyPair.RemoveRange(keyPairs);
                                await guardContext.SaveChangesAsync();
                            }
                        }

                        /* remove elliptic curve base points attributed to this app */
                        guardContext.BasePoint.RemoveRange(points);
                        await guardContext.SaveChangesAsync();

                        Domain? domain = await guardContext.Domain
                            .FirstOrDefaultAsync(e => e.Id == project.DomainId);

                        /* reindex domain's counter */
                        if (domain != null)
                        {
                            domain.count--;
                            await guardContext.SaveChangesAsync();
                        }

                        /* complete removing of the application */
                        guardContext.Application.Remove(project);
                        await guardContext.SaveChangesAsync();
                    }

                    /* no rights */
                    return false;
                }
            }

            /* project probably existed in the past */
            return null;
        }

        public async Task<ApplicationResultModel> GetApplicationsAsync(bool complete, string? userId, int? page)
        {
            int? uid = !string.IsNullOrEmpty(userId) ? Convert.ToInt32(userId) : null;
            int index = (page != null && page.Value >= 1) ? page.Value - 1 : 0;

            if (uid != null)
            {
                if (!complete)
                {
                    List<Application> apps = await guardContext.Application
                        .Where(p => p.AccountId == uid.Value && (p.IsDeleted != null ? !p.IsDeleted.Value : false))
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();

                    int counter = apps.Count;
                    int totalPages = counter >> 3;

                    if ((counter & 0x7) != 0)
                        totalPages++;

                    List<ApplicationViewModel> list = apps.Skip(8 * index).Take(8).ToList()
                        .Select(a => new ApplicationViewModel
                        {
                            Id = a.Id,
                            AppName = a.AppName,
                            ClientId = a.ClientId,
                            CreatedAt = a.CreatedAt,
                            ModifiedAt = (a.ModifiedAt != null ? a.ModifiedAt.Value : a.CreatedAt),
                            AppType = a.AppType
                        })
                        .ToList();

                    ApplicationResultModel result = new ApplicationResultModel
                    {
                        Pages = totalPages,
                        ApplicationViewModels = list.ToArray(),
                        Results = apps.Count
                    };

                    return result;
                }
                else
                {
                    List<Application> apps = await guardContext.Application
                        .Where(p => p.AccountId == uid.Value && (p.IsDeleted != null && p.IsDeleted.Value))
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();

                    int counter = apps.Count;
                    int totalPages = counter >> 3;

                    if ((counter & 0x7) != 0)
                        totalPages++;

                    List<ApplicationViewModel> list = apps.Skip(8 * index).Take(8).ToList()
                        .Select(a => new ApplicationViewModel
                        {
                            Id = a.Id,
                            AppName = a.AppName,
                            ClientId = a.ClientId,
                            CreatedAt = a.CreatedAt,
                            ModifiedAt = (a.ModifiedAt != null ? a.ModifiedAt.Value : a.CreatedAt),
                            AppType = a.AppType
                        })
                        .ToList();

                    ApplicationResultModel result = new ApplicationResultModel
                    {
                        Pages = totalPages,
                        ApplicationViewModels = list.ToArray(),
                        Results = apps.Count
                    };

                    return result;
                }
            }

            return null;
        }

        public async Task<bool> CreateApplicationAsync(ApplicationModel appModel)
        {
            Application? app = await guardContext.Application
                .FirstOrDefaultAsync(p => p.AppName == appModel.appName && (p.IsDeleted != null ? !p.IsDeleted.Value : false));

            if (app == null)
            {
                string appLogo = string.Empty;

                if (appModel.appLogo != null)
                {
                    appLogo = $"./Storage/Projects/{appModel.appLogo.FileName}";
                    var ms = new MemoryStream();
                    await appModel.appLogo.CopyToAsync(ms);
                    File.WriteAllBytes(appLogo, ms.ToArray());
                }

                string clientId = Guid.NewGuid().ToString();
                List<Domain> domains = await guardContext.Domain.ToListAsync();
                int index = 0, count = 1000000;

                for (int k = 0; k < domains.Count; k++)
                {
                    int counter = domains[k].count != null
                        ? domains[k].count.Value : 0;

                    if (count > counter)
                    {
                        count = counter;
                        index = k;
                    }
                }

                /* set domain counter */
                int total = domains[index].count != null
                    ? domains[index].count.Value + 1 : 1;

                domains[index].count = total;
                await guardContext.SaveChangesAsync();

                BigInteger a = new BigInteger(domains[index].a);
                BigInteger b = new BigInteger(domains[index].b);
                BigInteger p = new BigInteger(domains[index].p);

                BigInteger N = new BigInteger(domains[index].N);
                EllipticCurve curve = new EllipticCurve(a, b, p, N);

                BigInteger X = new BigInteger(domains[index].x);
                BigInteger Y = new BigInteger(domains[index].y);
                ECPoint basePoint = new ECPoint(X, Y);

                BigInteger t = BigInteger.Next(rand, 1, N - 1);
                ECPoint point = CryptoUtil.Multiply(curve, t, basePoint);

                byte[] buffer = t.ToByteArray();
                byte[] all = new byte[32];

                for (int i = 0; i < buffer.Length; i++)
                    all[i] = buffer[i];

                app = new Application
                {
                    AppName = appModel.appName,
                    Description = appModel.description,
                    AccountId = appModel.AccountId.Value,
                    ClientSecret = Convert.ToBase64String(all),
                    IsDeleted = false,
                    AppType = appModel.appType,
                    CreatedAt = DateTime.UtcNow,
                    DomainId = domains[index].Id,
                    ClientId = clientId
                };

                /* if appLogo does exists */
                if (!string.IsNullOrEmpty(appLogo))
                    app.AppLogo = appLogo;

				guardContext.Application.Add(app);
                await guardContext.SaveChangesAsync();

                BasePoint G = new BasePoint
                {
                    ApplicationId = app.Id,
                    CreatedAt = app.CreatedAt,
                    DomainId = domains[index].Id,
                    x = point.GetAffineX().ToString(),
                    y = point.GetAffineY().ToString(),
                    IsSuspicious = false,
                    IsDeleted = false
                };

                guardContext.BasePoint.Add(G);
                await guardContext.SaveChangesAsync();

                /* write new info log at database */
                string body = $"You've created a new app, named {app.AppName}.";
                await logService.CreateLogAsync(clientId, body, Models.LogLevel.Info);

                /* send demand when the flag is activated */
                if (appSettings.EnableDemandSending != null && appSettings.EnableDemandSending.Value)
                {
                    /* creates new demand that will be processed later */
                    Demand demand = new Demand
                    {
                        IssuedAt = DateTime.UtcNow
                    };

                    guardContext.Demand.Add(demand);
                    await guardContext.SaveChangesAsync();
                }

                return true;
            }

            return false;
        }

        public async Task UpdateDatabase(ECParams[]? args)
        {
            if(args != null && args.Length > 0)
            {
                List<Domain> domainList = new List<Domain>();

                foreach(ECParams arg in args)
                {
                    Domain? domain = await guardContext.Domain.FirstOrDefaultAsync(e => e.webcode.CompareTo(arg.Guid) == 0);

                    if(domain == null)
                    {
                        domain = new Domain
                        {
                            a = arg.a,
                            b = arg.b,
                            p = arg.p,
                            N = arg.N,
                            x = arg.x,
                            y = arg.y,
                            webcode = arg.Guid
                        };

                        domainList.Add(domain);
                    }
                }

                /* save only if ec domain parameters does not found */
                if(domainList.Count > 0)
                {
                    await guardContext.Domain.AddRangeAsync(domainList);
                    await guardContext.SaveChangesAsync();
                }
            }
        }
    }
}
