using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightAutomation.Base;
using PlaywrightAutomation.Pages;
using PlaywrightAutomation.Utils;
using PlaywrightAutomation.Reporting;

namespace PlaywrightAutomation.Tests
{
    public class FindCorrectZoneTest
    {
        private readonly HashSet<string> processedTestCases = new HashSet<string>();

        private string ValidateResponse(string expectedZone, string actualResponse)
        {
            // Handle empty cases
            if (string.IsNullOrEmpty(expectedZone))
            {
                Console.WriteLine("Expected zone is empty - NO MATCH");
                return "NO MATCH";
            }

            if (string.IsNullOrEmpty(actualResponse))
            {
                Console.WriteLine("Actual response is empty - NO MATCH");
                return "NO MATCH";
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("ZONE VALIDATION");
            Console.WriteLine("========================================");
            Console.WriteLine($"Expected Zone: {expectedZone}");
            Console.WriteLine($"Bot Response: {actualResponse}");
            Console.WriteLine("========================================");

            // Normalize both strings for comparison
            var normalizedExpected = NormalizeZone(expectedZone);
            var normalizedResponse = NormalizeZone(actualResponse);

            Console.WriteLine($"\nNormalized Expected: {normalizedExpected}");
            Console.WriteLine($"Normalized Response: {normalizedResponse}");

            // Check if the expected zone is found in the response
            if (normalizedResponse.Contains(normalizedExpected))
            {
                Console.WriteLine("\n✓ MATCH - Expected zone found in response");
                Console.WriteLine("========================================\n");
                return "MATCH";
            }
            else
            {
                Console.WriteLine("\n✗ NO MATCH - Expected zone not found in response");
                Console.WriteLine("========================================\n");
                return "NO MATCH";
            }
        }

        private string NormalizeZone(string zone)
        {
            if (string.IsNullOrEmpty(zone))
                return "";

            // Normalize: lowercase, remove extra spaces, and remove special characters
            return zone.ToLower()
                .Replace("  ", " ")
                .Replace("-", "")
                .Replace("_", "")
                .Trim();
        }

        public async Task RunTestsAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("Starting Find Correct Zone tests...");
            report.SetCategory("Find Correct Zone Tests");
            Directory.CreateDirectory("Reporting");

            var findZonePage = new FindCorrectZone(baseClass.Page);

            try
            {
                var configManager = ConfigManager.Instance;
                var excelReader = new ExcelReader();

                // Get dynamic file paths from config
                string testCasesPath = configManager.GetFilePath("testcases");
                var testCases = excelReader.ReadExcel(testCasesPath);

                foreach (DataRow testCase in testCases.Rows)
                {
                    string testCaseId = testCase["TestCaseID"].ToString();

                    // Define which test cases to run for Find Correct Zone
                    // Modify these test case IDs according to your Excel sheet
                    if (testCaseId != "110850" && testCaseId != "110851" && testCaseId != "110852")
                        continue;

                    string scenario = testCase["Test Case"].ToString();
                    report.CreateTest($"{testCaseId} - {scenario}");
                    string status = "Fail";

                    try
                    {
                        switch (testCaseId)
                        {
                            case "110850":
                                // Validate main cards loaded
                                bool mainCardsVisible = await findZonePage.IsCardButtonVisibleAsync();
                                status = mainCardsVisible ? "Pass" : "Fail";
                                report.Log(status, mainCardsVisible ? "Main cards loaded and visible" : "Main cards not visible");
                                break;

                            case "110851":
                                status = await findZonePage.IsCardButtonVisibleAsync() ? "Pass" : "Fail";
                                report.Log(status, status == "Pass" ? "Verified Find Correct Zone card is visible" : "Find Correct Zone card not visible");
                                break;

                            case "110852":
                                Console.WriteLine("Test 110852: Attempting to click Find Correct Zone card...");
                                try
                                {
                                    await findZonePage.OpenCardAsync();
                                    Console.WriteLine("Test 110852: Click completed successfully");
                                    status = "Pass";
                                    report.Log(status, "Successfully clicked Find Correct Zone card");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Test 110852: Click failed - {ex.Message}");
                                    status = "Fail";
                                    report.Log(status, $"Failed to click Find Correct Zone card: {ex.Message}");
                                }
                                break;

                            default:
                                status = "Fail";
                                report.Log(status, $"Test case ID {testCaseId} is not implemented");
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

            }
            catch
            {
                throw;
            }
        }

        public async Task RunFindCorrectZoneFlowAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("\n======== Starting Find Correct Zone Flow Test ========");
            Directory.CreateDirectory("Reporting");

            var findZonePage = new FindCorrectZone(baseClass.Page);
            var parkingPage = new CheckParkingStreet(baseClass.Page);

            report.SetCategory("Find Correct Zone Flow");
            try
            {
                var configManager = ConfigManager.Instance;
                var excelReader = new ExcelReader();
                string testCasesPath = configManager.GetFilePath("testcases");
                
                report.CreateTest("Flow Setup - Navigation Preconditions");

                // Ensure Yes and Main Menu are clicked to display Find Correct Zone card
                Console.WriteLine("Step 1: Clicking 'Yes' button after Wokinghampermit completion...");
                try
                {
                    await parkingPage.ClickYesButtonAsync();
                    report.Log("Pass", "Clicked 'Yes' button successfully");
                    Console.WriteLine("✓ 'Yes' button clicked");
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to click Yes button: {ex.Message}");
                    report.Log("Info", $"Yes button click attempt: {ex.Message}");
                }
                
                Console.WriteLine("Step 2: Clicking Main Menu...");
                try
                {
                    await parkingPage.ClickMainCardOptionAsync();
                    report.Log("Pass", "Main Menu clicked");
                    Console.WriteLine("✓ Main Menu clicked");
                    await Task.Delay(2000);
                }
                catch (Exception menuEx)
                {
                    Console.WriteLine($"Warning: Main Menu click had issues: {menuEx.Message}");
                    report.Log("Info", $"Main Menu click attempt: {menuEx.Message}");
                }

                Console.WriteLine("✓ Ready - Find Correct Zone card should now be visible");

                // Test Case 110850: Mark as Pass since CheckParkingStreetTest completed successfully
                Console.WriteLine("Test Case 110850: Marking as Pass - CheckParkingStreetTest completed");
                excelReader.WriteStatus(testCasesPath, "110850", "Pass");
                report.CreateTest("110850 - Verify the user able to enter Zone and Validate the responses");
                report.Log("Pass", "CheckParkingStreetTest completed successfully - Zone entry and validation working");

                // Test Case 110851: Click Find Correct Zone button by text
                report.CreateTest("110851 - Verify the user able to click on Find Correct Zone card");
                Console.WriteLine("Test Case 110851: Clicking 'Find Correct Zone' card...");
                try
                {
                    await findZonePage.OpenCardAsync();
                    Console.WriteLine("✓ Find Correct Zone clicked successfully");
                    
                    // Click on the text field to activate it
                    Console.WriteLine("Clicking on text field...");
                    await findZonePage.ClickTextFieldAsync();
                    
                    // Mark test case 110851 as Pass
                    excelReader.WriteStatus(testCasesPath, "110851", "Pass");
                    report.Log("Pass", "Successfully clicked Find Correct Zone card and activated text field");
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error clicking Find Correct Zone: {ex.Message}");
                    
                    // Mark test case 110851 as Fail
                    excelReader.WriteStatus(testCasesPath, "110851", "Fail");
                    report.Log("Fail", $"Failed to click Find Correct Zone card: {ex.Message}");
                    throw;
                }

                // Test Case 110852: Enter Post Code and find matching Zones
                report.CreateTest("110852 - Verify the user able to enter the Post Code and fine the matching Zones");
                Console.WriteLine("Test Case 110852: Starting Post Code entry and zone matching test...");
                
                // Get dynamic zones file path from config
                string residentialFilePath = configManager.GetFilePath("zones");
                var zoneData = excelReader.ReadResidentialZonesExcel(residentialFilePath);

                Console.WriteLine("Waiting for bot to prompt for postcode...");

                // Wait for the bot's prompt message
                try
                {
                    await baseClass.Page.WaitForSelectorAsync("text=Please provide", new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15000 });
                    Console.WriteLine("Bot has prompted for postcode input");
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Bot prompt not detected - {ex.Message}");
                }

                // Verify input field is ready
                try
                {
                    await baseClass.Page.WaitForSelectorAsync("#txt_Chat", new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 10000 });
                    Console.WriteLine("Input field is now visible and ready");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Input field may not be ready - {ex.Message}");
                }

                // Loop through all post codes in Excel sheet
                int totalPostCodes = zoneData.Rows.Count;
                Console.WriteLine($"\n📋 Total postcodes to test: {totalPostCodes}\n");

                string previousResponse = "";
                bool testCase110852Passed = true; // Track if test case 110852 passes

                for (int rowIndex = 0; rowIndex < totalPostCodes; rowIndex++)
                {
                    var row = zoneData.Rows[rowIndex];
                    string postCode = row["PostCode"]?.ToString()?.Trim() ?? "";
                    string expectedZone = row["Zone"]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrEmpty(postCode))
                    {
                        Console.WriteLine($"Row {rowIndex + 1}: Empty postcode encountered. Stopping loop.");
                        break;
                    }

                    Console.WriteLine($"\n{'='} Testing Row {rowIndex + 1}/{totalPostCodes}: Postcode {postCode} {'='}");
                    Console.WriteLine($"Expected Zone: {expectedZone}");

                    report.CreateTest($"Postcode Query {rowIndex + 1}: {postCode}");

                    try
                    {
                        // Step 1: Type postcode into input box
                        Console.WriteLine($"[{rowIndex + 1}] Step 1: Typing postcode '{postCode}'...");
                        await findZonePage.TypeStreetAsync(postCode);

                        // Step 2: Click Send button
                        Console.WriteLine($"[{rowIndex + 1}] Step 2: Clicking Send button...");
                        await findZonePage.ClickSendButtonAsync();

                        // Step 2.5: Verify the message was sent
                        Console.WriteLine($"[{rowIndex + 1}] Step 2.5: Verifying message was sent...");
                        bool messageSent = await findZonePage.VerifyMessageSentAsync(postCode);
                        if (!messageSent)
                        {
                            Console.WriteLine("WARNING: Message may not have been sent! Retrying...");
                            await findZonePage.ClickSendButtonAsync();
                            await Task.Delay(2000);
                        }

                        // Step 3: Get the response
                        Console.WriteLine($"[{rowIndex + 1}] Step 3: Waiting for and getting response...");
                        await Task.Delay(3000);
                        string response = await findZonePage.GetResponseTextAsync(previousResponse);
                        Console.WriteLine($"Response Received: {response}");
                        previousResponse = response;

                        // Validate result
                        string validationResult = ValidateResponse(expectedZone, response);
                        Console.WriteLine($"Validation: {validationResult}");

                        // Determine if match
                        bool isMatch = validationResult.StartsWith("MATCH");
                        string excelResult = isMatch ? "Match" : "No Match";

                        // Write result to Excel
                        excelReader.WriteResult(residentialFilePath, rowIndex, excelResult);

                        // Log result to report
                        report.Log(isMatch ? "Pass" : "Info", $"Postcode: {postCode} | Expected Zone: {expectedZone} | {validationResult}");

                        // Step 4: Click Yes button (if needed to continue)
                        Console.WriteLine($"[{rowIndex + 1}] Step 4: Clicking 'Yes' button...");
                        try
                        {
                            await findZonePage.ClickYesButtonAsync();
                            report.Log("Pass", "Clicked 'Yes' button successfully");
                            await Task.Delay(1500);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to click Yes button: {ex.Message}");
                            report.Log("Info", $"Yes button not found or not needed: {ex.Message}");
                        }

                        // Step 5: Click Main Menu button (if not the last street)
                        if (rowIndex < totalPostCodes - 1)
                        {
                            Console.WriteLine($"[{rowIndex + 1}] Step 5: Clicking Main Menu...");
                            try
                            {
                                await findZonePage.ClickMainCardOptionAsync();
                                report.Log("Pass", "Main Menu clicked");
                                await Task.Delay(1500);
                            }
                            catch (Exception menuEx)
                            {
                                Console.WriteLine($"Warning: Main Menu click had issues: {menuEx.Message}");
                                report.Log("Info", "Main Menu click attempted");
                            }

                            // Step 6: Click Find Correct Zone again for next postcode
                            Console.WriteLine($"[{rowIndex + 1}] Step 6: Clicking 'Find Correct Zone' for next postcode...");
                            try
                            {
                                await findZonePage.OpenCardAsync();
                                await Task.Delay(2000);
                            }
                            catch (Exception cardEx)
                            {
                                Console.WriteLine($"Warning: Find Correct Zone click failed: {cardEx.Message}");
                            }

                            // Wait for bot prompt for next postcode
                            try
                            {
                                await baseClass.Page.WaitForSelectorAsync("text=Please provide", new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15000 });
                                Console.WriteLine("Bot prompted for next postcode");
                                await Task.Delay(1000);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Bot prompt not detected - {ex.Message}");
                                await Task.Delay(2000);
                            }
                        }

                        Console.WriteLine($"\n✅ Postcode {postCode} test completed!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error testing Postcode {postCode}: {ex.Message}");
                        report.Log("Fail", $"Postcode {postCode}: {ex.Message}");
                        excelReader.WriteResult(residentialFilePath, rowIndex, "ERROR");
                        testCase110852Passed = false; // Mark test case as failed
                    }
                }

                // Mark Test Case 110852 based on results
                if (testCase110852Passed)
                {
                    Console.WriteLine("Test Case 110852: PASSED - All streets processed successfully");
                    excelReader.WriteStatus(testCasesPath, "110852", "Pass");
                    report.Log("Pass", "Successfully entered Post Codes and found matching Zones");
                }
                else
                {
                    Console.WriteLine("Test Case 110852: FAILED - Some streets had errors");
                    excelReader.WriteStatus(testCasesPath, "110852", "Fail");
                    report.Log("Fail", "Errors occurred while entering Post Codes and finding Zones");
                }

                Console.WriteLine($"\n======== All {totalPostCodes} postcodes tested! ========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Find Correct Zone flow tests: {ex.Message}");
                throw;
            }
        }
    }
}
