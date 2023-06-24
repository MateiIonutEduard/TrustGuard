using TrustGuard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrustGuard.Models;
using TrustGuard.Services;

namespace TrustGuard
{
    public class Program
    {
        public static void Main(string[] args)
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

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}