﻿using Eduard;
using TrustGuard.Data;
using TrustGuard.Environment;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TrustGuard.Models;
using TrustGuard.Core;
using ECPoint = Eduard.ECPoint;
#pragma warning disable

namespace TrustGuard.Services
{
    public class ApplicationService : IApplicationService
    {
        readonly TrustGuardContext guardContext;
        readonly RandomNumberGenerator rand;

        public ApplicationService(TrustGuardContext guardContext)
        { 
            this.guardContext = guardContext;
            rand = RandomNumberGenerator.Create();
        }

        public async Task<bool> CreateProductAsync(ApplicationModel appModel)
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

                if (!string.IsNullOrEmpty(appLogo))
                {
                    string clientId = Guid.NewGuid().ToString();
                    List<Domain> domains = await guardContext.Domain.ToListAsync();
                    int index = 0, count = 1000000;

                    for(int k = 0; k < domains.Count; k++)
                    {
                        int counter = domains[k].count != null 
                            ? domains[k].count.Value : 0;

                        if(count > counter)
                        {
                            count = counter;
                            index = k;
                        }
                    }

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
                        AppLogo = appLogo,
                        AppType = appModel.appType,
                        CreatedAt = DateTime.UtcNow,
                        DomainId = domains[index].Id,
                        ClientId = clientId
                    };

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
                    return true;
                }
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
