using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Base
{
    public class BaseClass
    {
        public IBrowser Browser { get; private set; }
        public IPage Page { get; private set; }
        public IBrowserContext Context { get; private set; }

        public async Task InitBrowserAsync()
        {
            var playwright = await Playwright.CreateAsync();
            Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
            { 
                Headless = false,
                Channel = "chrome",
                Args = new[] { 
                    "--window-size=1024,768",  // More conservative size
                    "--window-position=0,0",
                    "--disable-gpu"
                }
            });
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions 
            { 
                ViewportSize = new ViewportSize { Width = 1024, Height = 768 }
            });
            Page = await Context.NewPageAsync();
            // Set viewport size directly
            await Page.SetViewportSizeAsync(1024, 768);
        }

        public async Task CloseBrowserAsync()
        {
            await Browser.CloseAsync();
        }
    }
}
