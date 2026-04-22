using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using PlaywrightAutomation.Base;
using PlaywrightAutomation.Pages;
using PlaywrightAutomation.Utils;
using PlaywrightAutomation.Reporting;

namespace PlaywrightAutomation.Tests
{
    public class LoginTest
    {
        private readonly HashSet<string> processedTestCases = new HashSet<string>();

        public async Task RunTestsAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("Starting Login tests...");
            report.SetCategory("Login Tests");
            Directory.CreateDirectory("Reporting");

            var loginPage = new LoginPage(baseClass.Page);

            try
            {
                var configManager = ConfigManager.Instance;
                var excelReader = new ExcelReader();

                // Get dynamic file paths from config
                string testDataPath = configManager.GetFilePath("testdata");
                string testCasesPath = configManager.GetFilePath("testcases");

                var testData = excelReader.ReadExcel(testDataPath);
                var testCases = excelReader.ReadExcel(testCasesPath);

                // Get user details from config (if Excel doesn't have them)
                var userDetails = configManager.GetUserDetails();
                var dataRow = testData.Rows[0];

                // Use URL from Excel if available, otherwise use config URL
                string loginUrl = dataRow["LoginUrl"]?.ToString();
                if (string.IsNullOrEmpty(loginUrl) || loginUrl == "LOGIN_URL")
                {
                    loginUrl = configManager.GetActiveCouncilUrl();
                }
                await loginPage.OpenUrlAsync(loginUrl);

                foreach (DataRow testCase in testCases.Rows)
                {
                    string testCaseId = testCase["TestCaseID"].ToString();

                    if (processedTestCases.Contains(testCaseId))
                    {
                        Console.WriteLine($"Skipping duplicate test case: {testCaseId}");
                        continue;
                    }

                    processedTestCases.Add(testCaseId);

                    // Skip test cases that belong to CheckParkingStreetTest
                    if (testCaseId == "110847" || testCaseId == "110848" || testCaseId == "110849")
                        continue;

                    string scenario = testCase["Test Case"].ToString();
                    Console.WriteLine($"Executing Test Case: {testCaseId} - {scenario}");

                    report.CreateTest($"{testCaseId} - {scenario}");
                    string status = "Fail";

                    try
                    {
                        switch (testCaseId)
                        {
                            case "96937":
                                status = await loginPage.IsChatBotLoadedAsync() ? "Pass" : "Fail";
                                report.Log(status, "Verified Permit bot URL loaded and chat opened");
                                break;

                            case "110841":
                                status = await loginPage.IsGreetingMessageVisibleAsync() ? "Pass" : "Fail";
                                report.Log(status, "Verified greeting message is visible");
                                break;

                            case "110842":
                                status = await loginPage.IsAdaptiveCardFormVisibleAsync() ? "Pass" : "Fail";
                                report.Log(status, "Verified adaptive card form fields");
                                break;

                            case "110843":
                                await loginPage.FillFormAndSubmit(dataRow["FirstName"].ToString(), "", "");
                                status = "Pass";
                                report.Log(status, "Entered First Name");
                                break;

                            case "110844":
                                await loginPage.FillFormAndSubmit("", dataRow["LastName"].ToString(), "");
                                status = "Pass";
                                report.Log(status, "Entered Last Name");
                                break;

                            case "110845":
                                await loginPage.FillFormAndSubmit("", "", dataRow["MobileNo"].ToString());
                                status = "Pass";
                                report.Log(status, "Entered Mobile Number");
                                break;

                            case "110846":
                                await loginPage.FillFormAndSubmit(
                                    dataRow["FirstName"].ToString(),
                                    dataRow["LastName"].ToString(),
                                    dataRow["MobileNo"].ToString()
                                );
                                status = "Pass";
                                report.Log(status, "Submitted form with valid details");
                                break;

                            case "110847":
                                await loginPage.FillForm(
                                    dataRow["FirstName"].ToString(),
                                    dataRow["LastName"].ToString(),
                                    dataRow["MobileNo"].ToString()
                                );
                                await loginPage.ClickSubmit();

                                // ✅ FIXED MAIN CARD VALIDATION
                                bool mainCardsVisible = await loginPage.IsMainCardOptionsVisible();

                                if (!mainCardsVisible)
                                {
                                    status = "Fail";
                                    report.Log("Fail", "Main card options not visible after form submission");
                                    break;
                                }

                                string[] expectedOptions =
                                {
                                    "Check Parking Streets",
                                    "Check Permit Status",
                                    "Find Correct Zone",
                                    "General Queries",
                                    "Account Management",
                                    "Payment & Fee"
                                };

                                bool allOptionsPresent = true;

                                foreach (var option in expectedOptions)
                                {
                                    var locator = baseClass.Page
                                        .Locator($"text={option}")
                                        .First;

                                    if (!await locator.IsVisibleAsync())
                                    {
                                        allOptionsPresent = false;
                                        report.Log("Fail", $"Option '{option}' not visible");
                                    }
                                    else
                                    {
                                        report.Log("Pass", $"Option '{option}' visible");
                                    }
                                }

                                status = allOptionsPresent ? "Pass" : "Fail";
                                break;

                            default:
                                status = "Skip";
                                report.Log("Skip", "Test case not applicable for Login flow");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        status = "Fail";
                        report.Log("Fail", ex.Message);
                    }

                    excelReader.WriteStatus(testCasesPath, testCaseId, status);
                }

                // Do not close the browser here to allow continuation in CheckParkingStreetTest
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test execution: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to get the dynamic test cases file path
        /// </summary>
        public string GetTestCasesPath()
        {
            return ConfigManager.Instance.GetFilePath("testcases");
        }
    }
}
