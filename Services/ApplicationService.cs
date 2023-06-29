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
        readonly IJwtSettings jwtSettings;
        readonly TrustGuardContext guardContext;
        readonly RandomNumberGenerator rand;

        public ApplicationService(IJwtSettings jwtSettings, TrustGuardContext guardContext)
        { 
            this.guardContext = guardContext;
            rand = RandomNumberGenerator.Create();
            this.jwtSettings = jwtSettings;
        }

        public async Task<int> RevokeTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret)
        {
            KeyPair keyPair = await guardContext.KeyPair
                .FirstOrDefaultAsync(e => e.AccessToken.CompareTo(accessToken) == 0);

            if(keyPair != null && !keyPair.IsRevoked)
            {
                BasePoint basePoint = await guardContext.BasePoint
                    .FirstOrDefaultAsync(e => e.Id == keyPair.BasePointId);

                // domain parameters
                Domain domain = await guardContext.Domain
                    .FirstOrDefaultAsync(e => e.Id == basePoint.DomainId);

                BigInteger a = new BigInteger(domain.a.ToString());
                BigInteger b = new BigInteger(domain.b.ToString());

                BigInteger p = new BigInteger(domain.p.ToString());
                BigInteger N = new BigInteger(domain.N.ToString());
                EllipticCurve curve = new EllipticCurve(a, b, p, N);

                BigInteger x = new BigInteger(basePoint.x.ToString());
                BigInteger y = new BigInteger(basePoint.y.ToString());

                ECPoint G = new ECPoint(x, y);
                TokenFactory tokenFactory = new TokenFactory(curve, G);

                /* revoke key pair */
                keyPair.IsRevoked = true;
                await guardContext.SaveChangesAsync();

                int result = tokenFactory.VerifyToken(refreshToken, accessToken);
                return result;
            }

            /* already revoked */
            return -2;
        }

        public async Task<TokenViewModel?> RefreshTokenAsync(string refreshToken, string accessToken, string? clientId, string? clientSecret)
        {
            Application? application = await guardContext.Application
                .FirstOrDefaultAsync(e => e.ClientId.CompareTo(clientId) == 0 && e.ClientSecret.CompareTo(clientSecret) == 0);

            if(application != null)
            {
                BasePoint basePoint = await guardContext.BasePoint
                    .FirstOrDefaultAsync(e => e.ApplicationId == application.Id && (e.IsDeleted == null || (e.IsDeleted != null && !e.IsDeleted.Value)));

                Domain domain = await guardContext.Domain
                    .FirstOrDefaultAsync(e => e.Id == application.DomainId);

                KeyPair keyPair = await guardContext.KeyPair
                    .FirstOrDefaultAsync(e => e.AccessToken.CompareTo(accessToken) == 0 && !e.IsRevoked && e.BasePointId == basePoint.Id);

                if(keyPair != null)
                {
                    BigInteger a = new BigInteger(domain.a.ToString());
                    BigInteger b = new BigInteger(domain.b.ToString());

                    BigInteger p = new BigInteger(domain.p.ToString());
                    BigInteger N = new BigInteger(domain.N.ToString());
                    EllipticCurve curve = new EllipticCurve(a, b, p, N);

                    BigInteger x = new BigInteger(basePoint.x.ToString());
                    BigInteger y = new BigInteger(basePoint.y.ToString());
                    ECPoint G = new ECPoint(x, y);

                    /* verify if valid signature */
                    TokenFactory tokenFactory = new TokenFactory(curve, G);
                    int res = tokenFactory.VerifyToken(refreshToken, accessToken);

                    // get user account
                    Account account = await guardContext.Account
                        .FirstOrDefaultAsync(e => e.Id == keyPair.AccountId);

                    if(res == 1)
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

                        TokenModel model = tokenFactory.SignToken(desc);
                        KeyPair newKeyPair = new KeyPair
                        {
                            SecureKey = model.secretKey,
                            RefreshToken = model.refresh_token,
                            AccessToken = model.access_token,
                            BasePointId = basePoint.Id,
                            AccountId = account.Id,
                            IsRevoked = false
                        };

                        /* save keypair to database */
                        guardContext.KeyPair.Add(newKeyPair);
                        await guardContext.SaveChangesAsync();

                        TokenViewModel token = new TokenViewModel
                        {
                            access_token = model.access_token,
                            refresh_token = model.refresh_token
                        };

                        return token;
                    }
                }
            }
            
            return null;
        }

        public async Task<TokenViewModel?> AuthenticateAsync(string? userId, string? clientId, string? clientSecret)
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
                        .FirstOrDefaultAsync(e => e.ClientId.CompareTo(clientId) == 0 && e.ClientSecret.CompareTo(clientSecret) == 0);

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

                        tokenModel = tokenFactory.SignToken(desc);
                        KeyPair keyPair = new KeyPair
                        {
                            SecureKey = tokenModel.secretKey,
                            RefreshToken = tokenModel.refresh_token,
                            AccessToken = tokenModel.access_token,
                            BasePointId = basePoint.Id,
                            AccountId = account.Id,
                            IsRevoked = false
                        };

                        /* save keypair to database */
                        guardContext.KeyPair.Add(keyPair);
                        await guardContext.SaveChangesAsync();

                        TokenViewModel token = new TokenViewModel
                        {
                            access_token = tokenModel.access_token,
                            refresh_token = tokenModel.refresh_token
                        };

                        return token;
                    }
                }
            }

            return null;
        }

        public async Task<Application?> GetApplicationAsync(int id)
        {
            Application? application = await guardContext.Application
                .FirstOrDefaultAsync(p => p.Id == id);

            return application;
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
                        if (!apps[i].AppName.StartsWith(filter.AppName))
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

                int counter = apps.Count;
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
                    Results = apps.Count
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

                /* creates new demand that will be processed later */
                Demand demand = new Demand
                {
                    IssuedAt = DateTime.UtcNow
                };

                guardContext.Demand.Add(demand);
                await guardContext.SaveChangesAsync();
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
