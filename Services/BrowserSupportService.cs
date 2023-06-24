using Newtonsoft.Json;
using TrustGuard.Models;
using UAParser;
#pragma warning disable

namespace TrustGuard.Services
{
    public class BrowserSupportService : IBrowserSupportService
    {
        readonly Parser uaParser;
        readonly IAppSettings appSettings;

        public BrowserSupportService(IAppSettings appSettings)
        {
            uaParser = Parser.GetDefault();
            this.appSettings = appSettings;
        }

        public bool IsBrowserSupported(string userAgent)
        {
            ClientInfo c = uaParser.Parse(userAgent);
            Version currentVersion = Version.Parse($"{c.UA.Major}.{c.UA.Minor}");

            BrowserModel browserModel = new BrowserModel
            {
                BrowserName = c.UA.Family,
                BrowserVersion = currentVersion
            };

            /* if browser support enabled */
            if (appSettings.EnableBrowserSupport != null && appSettings.EnableBrowserSupport.Value)
            {
                string data = File.ReadAllText("./browserSupport.json");
                BrowserDataView[]? browsers = JsonConvert.DeserializeObject<BrowserDataView[]>(data);

                if (browsers != null && browsers.Length > 0)
                {
                    BrowserDataView browser = browsers.FirstOrDefault(b => b.Name.CompareTo(browserModel.BrowserName) == 0);
                    Version browserVersion = Version.Parse(browser.Version);

                    if (browser != null && currentVersion.CompareTo(browserVersion) > 0) return true;
                    return false;
                }
            }

            return true;
        }
    }
}
