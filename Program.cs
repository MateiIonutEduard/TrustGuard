using TrustGuard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrustGuard.Models;
using TrustGuard.Services;
using System.Data;
using TrustGuard.Environment;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TrustGuard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add PostgreSQL database linker to entities.
            builder.Services.AddEntityFrameworkNpgsql().AddDbContext<TrustGuardContext>(opt =>
                opt.UseNpgsql(builder.Configuration.GetConnectionString("TrustGuard")));

            // get appSettings section
            builder.Services.Configure<AppSettings>(
                    builder.Configuration.GetSection(nameof(AppSettings)));

            // register AppSettings as singleton service
            builder.Services.AddSingleton<IAppSettings>(sp =>
                sp.GetRequiredService<IOptions<AppSettings>>().Value);

            /* admin smtp server settings */
            builder.Services.Configure<AdminSettings>(
                    builder.Configuration.GetSection(nameof(AdminSettings)));

            // register smtp settings as singleton service
            builder.Services.AddSingleton<IAdminSettings>(sp =>
                sp.GetRequiredService<IOptions<AdminSettings>>().Value);

            /* declares admin service, as singleton service */
            builder.Services.AddSingleton<IAdminService, AdminService>();

            // declares browser support service
            builder.Services.AddSingleton<IBrowserSupportService, BrowserSupportService>();

            // add crypto service required at password encryption
            builder.Services.AddSingleton<ICryptoService, CryptoService>();

			// add account service, with all features
			builder.Services.AddTransient<IAccountService, AccountService>();

			// register application service
			builder.Services.AddTransient<IApplicationService, ApplicationService>();

			// Add cookie authentication
			builder.Services.AddAuthentication("CookieAuthentication")
				.AddCookie("CookieAuthentication", config =>
				{
					config.Cookie.Name = "AuthCookie";
					config.LoginPath = "/Account/";
				});

            /* register session service */
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            /* check if elliptic curves are currently inserted in database */
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await Guard.UpdateDomainParameters(services);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}