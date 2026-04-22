using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightAutomation.Tests;
using PlaywrightAutomation.Base;
using PlaywrightAutomation.Reporting;
using PlaywrightAutomation.Utils;

namespace PlaywrightAutomation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load configuration from config.json
            var configManager = ConfigManager.Instance;
            configManager.LoadConfig("config.json");

            // Check if there's an active council to run
            if (!configManager.HasActiveCouncil())
            {
                Console.WriteLine("No council configured with 'run: true'. Exiting.");
                Console.WriteLine("Please set 'run: true' for the desired council in config.json");
                return;
            }

            var baseClass = new BaseClass();
            await baseClass.InitBrowserAsync();

            // Single consolidated report for all test flows
            var report = new ExtentReport();
            report.InitReport("TestReport.html");

            var loginTest = new LoginTest();
            await loginTest.RunTestsAsync(baseClass, report);

            var checkParkingStreetTest = new CheckParkingStreetTest();
            await checkParkingStreetTest.RunTestsAsync(baseClass, report);

            // Run permit zone tests (dynamic council file)
            await checkParkingStreetTest.RunPermitZoneTestsAsync(baseClass, report);

            // Run Find Correct Zone flow tests
            var findCorrectZoneTest = new FindCorrectZoneTest();
            await findCorrectZoneTest.RunFindCorrectZoneFlowAsync(baseClass, report);

            // Run General Queries flow tests
            var generalQueriesTest = new GeneralQueriesTest();
            await generalQueriesTest.RunGeneralQueriesFlowAsync(baseClass, report);

            // Run OtherMenu flow tests (reads Sheet2 "Menu" from queries file)
            var otherMenuTest = new OtherMenuTest();
            await otherMenuTest.RunOtherMenuFlowAsync(baseClass, report);

            // Run Permit Status flow tests
            var permitStatusTest = new PermitStatusTest();
            await permitStatusTest.RunPermitStatusFlowAsync(baseClass, report);

            // Flush the single consolidated report
            report.Flush();

            await baseClass.CloseBrowserAsync();
        }
    }
}
