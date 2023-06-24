using System.Net.Mail;
using System.Net;
using TrustGuard.Models;

namespace TrustGuard.Services
{
    public class AdminService : IAdminService
    {
        readonly IAdminSettings adminSettings;
        public AdminService(IAdminSettings adminSettings)
        { this.adminSettings = adminSettings; }

        /* Returns status code after sending email. */
        public int SendEmail(string to, string subject, string body)
        {
            /* set up admin settings */
            if (adminSettings.port == null 
                || string.IsNullOrEmpty(adminSettings.host) 
                || string.IsNullOrEmpty(adminSettings.client) 
                || string.IsNullOrEmpty(adminSettings.secret)) return -1;

            try
            {
                int port = adminSettings.port.Value;
                SmtpClient host = new SmtpClient(adminSettings.host, port);
                host.EnableSsl = true;

                host.UseDefaultCredentials = false;
                host.Credentials = new NetworkCredential(adminSettings.client, adminSettings.secret);
                MailMessage mail = new MailMessage();

                mail.To.Add(to);
                mail.From = new MailAddress(adminSettings.client);
                mail.Subject = subject;

                mail.Body = body;
                mail.IsBodyHtml = true;

                // mail sent successfully
                host.Send(mail);
                return 1;
            }
            catch (Exception)
            {
                // probably, smtp server is not configured
                return 0;
            }
        }
    }
}
