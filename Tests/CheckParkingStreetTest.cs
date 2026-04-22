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
    public class CheckParkingStreetTest
    {
        private readonly HashSet<string> processedTestCases = new HashSet<string>();

        private string ValidateResponse(string expectedStreetName, string actualResponse)
        {
            // Handle empty cases
            if (string.IsNullOrEmpty(expectedStreetName))
            {
                Console.WriteLine("Expected street name is empty - NO MATCH");
                return "NO MATCH";
            }

            if (string.IsNullOrEmpty(actualResponse))
            {
                Console.WriteLine("Actual response is empty - NO MATCH");
                return "NO MATCH";
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("STREET NAME VALIDATION");
            Console.WriteLine("========================================");
            Console.WriteLine($"Expected Street(s): {expectedStreetName}");
            Console.WriteLine($"Bot Response Length: {actualResponse.Length} chars");
            Console.WriteLine("========================================");

            // Parse expected street names (split by newlines only)
            var expectedStreets = expectedStreetName
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => NormalizeStreetName(s.Trim()))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            Console.WriteLine($"\nExpected ({expectedStreets.Count} streets):");
            foreach (var street in expectedStreets)
            {
                Console.WriteLine($"  - {street}");
            }

            // Extract ONLY street names from response (filter out all non-street text)
            // Split response into lines and clean each one
            var responseLines = actualResponse
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Select(line => line.TrimStart('•', '-', '*', '·', '○', '●', ' ', '\t'))
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => 
                {
                    var lower = line.ToLower();
                    // Exclude lines that are clearly not street names
                    return !lower.Contains("here are") &&
                           !lower.Contains("parking street") &&
                           !lower.Contains("valid for") &&
                           !lower.Contains("zone") &&
                           !lower.Contains("is there anything") &&
                           !lower.Contains("help you") &&
                           !lower.Contains("return null") &&
                           !lower.Contains("function") &&
                           !lower.Contains("document.") &&
                           !lower.Contains("var ") &&
                           !lower.Contains("let ") &&
                           !lower.StartsWith("//") &&
                           line.Length < 100; // Street names shouldn't be too long
                })
                .Select(line => NormalizeStreetName(line))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            Console.WriteLine($"\nExtracted from Response ({responseLines.Count} streets):");
            foreach (var street in responseLines)
            {
                Console.WriteLine($"  - {street}");
            }

            if (!expectedStreets.Any())
            {
                Console.WriteLine("\n✗ No expected streets to validate");
                Console.WriteLine("========================================\n");
                return "NO MATCH";
            }

            if (!responseLines.Any())
            {
                Console.WriteLine("\n✗ No street names found in response");
                Console.WriteLine("========================================\n");
                return "NO MATCH";
            }

            // Check each expected street
            Console.WriteLine($"\nValidating:");
            int matchCount = 0;
            
            foreach (var expectedStreet in expectedStreets)
            {
                bool found = responseLines.Contains(expectedStreet);
                
                if (found)
                {
                    Console.WriteLine($"  ✓ '{expectedStreet}' - FOUND");
                    matchCount++;
                }
                else
                {
                    Console.WriteLine($"  ✗ '{expectedStreet}' - NOT FOUND");
                }
            }

            Console.WriteLine($"\nResult: {matchCount}/{expectedStreets.Count} matched");
            
            // ALL expected streets must be found
            if (matchCount == expectedStreets.Count)
            {
                Console.WriteLine("✓ MATCH - All expected streets found");
                Console.WriteLine("========================================\n");
                return "MATCH";
            }
            else
            {
                Console.WriteLine("✗ NO MATCH - Some expected streets missing");
                Console.WriteLine("========================================\n");
                return "NO MATCH";
            }
        }

        private string NormalizeStreetName(string streetName)
        {
            if (string.IsNullOrEmpty(streetName))
                return "";

            // Simple normalization: lowercase and remove extra spaces only
            return streetName.ToLower()
                .Replace("  ", " ")
                .Trim();
        }

        public async Task RunTestsAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("Starting Check Parking Street tests...");
            report.SetCategory("Check Parking Street Tests");
            Directory.CreateDirectory("Reporting");

            var parkingPage = new CheckParkingStreet(baseClass.Page);

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

                    // Only run test cases 110847, 110848, and 110849
                    if (testCaseId != "110847" && testCaseId != "110848" && testCaseId != "110849")
                        continue;

                    string scenario = testCase["Test Case"].ToString();
                    report.CreateTest($"{testCaseId} - {scenario}");
                    string status = "Fail";

                    try
                    {
                        switch (testCaseId)
                        {
                            case "110847":
                                // Validate main cards loaded
                                bool mainCardsVisible = await parkingPage.IsCardButtonVisibleAsync();
                                status = mainCardsVisible ? "Pass" : "Fail";
                                report.Log(status, mainCardsVisible ? "Main cards loaded and visible" : "Main cards not visible");
                                break;

                            case "110848":
                                status = await parkingPage.IsCardButtonVisibleAsync() ? "Pass" : "Fail";
                                report.Log(status, status == "Pass" ? "Verified Check Parking Street card is visible" : "Check Parking Street card not visible");
                                break;

                            case "110849":
                                Console.WriteLine("Test 110849: Attempting to click Check Parking Street card...");
                                try
                                {
                                    await parkingPage.OpenCardAsync();
                                    Console.WriteLine("Test 110849: Click completed successfully");
                                    status = "Pass";
                                    report.Log(status, "Successfully clicked Check Parking Street card");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Test 110849: Click failed - {ex.Message}");
                                    status = "Fail";
                                    report.Log(status, $"Failed to click Check Parking Street card: {ex.Message}");
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

                // Do not close the browser here to allow further tests if needed
            }
            catch
            {
                throw;
            }
        }

        public async Task RunPermitZoneTestsAsync(BaseClass baseClass, ExtentReport report)
        {
            var configManager = ConfigManager.Instance;
            var activeCouncil = configManager.GetActiveCouncil();
            string councilName = activeCouncil?.Name ?? "Unknown";

            Console.WriteLine($"\n======== Starting {councilName} Permit Zone Test ========");
            report.SetCategory($"{councilName} Permit Zone Tests");
            Directory.CreateDirectory("Reporting");

            var parkingPage = new CheckParkingStreet(baseClass.Page);

            try
            {
                var excelReader = new ExcelReader();

                // Get dynamic permit file path from config
                string permitFilePath = configManager.GetFilePath("permit");
                var zoneData = excelReader.ReadWokinhampermitExcel(permitFilePath);

                // Card is already clicked from previous test, wait for input to be ready
                Console.WriteLine("Checking current state after previous test...");
                
                // Check if main menu is showing - if so, click "Check Parking Streets" again
                var mainMenuVisible = await baseClass.Page.Locator("button:has-text('Check Parking Streets')").CountAsync();
                if (mainMenuVisible > 0)
                {
                    Console.WriteLine("Main menu is visible - clicking 'Check Parking Streets' card to start flow");
                    await parkingPage.OpenCardAsync();
                    await Task.Delay(2000); // Wait for transition
                }
                
                Console.WriteLine("Waiting for bot to prompt for zone...");
                
                // Wait for the bot's prompt message "Great! Please provide your zone."
                try
                {
                    await baseClass.Page.WaitForSelectorAsync("text=Great! Please provide your zone.", new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15000 });
                    Console.WriteLine("Bot has prompted for zone input");
                    await Task.Delay(1000); // Extra stability wait
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

                // Loop through all zones in Excel sheet
                int totalZones = zoneData.Rows.Count;
            Console.WriteLine($"\n📋 Total zones to test: {totalZones}\n");
            
            string previousResponse = ""; // Track previous response to avoid getting stale data
            
            for (int rowIndex = 0; rowIndex < totalZones; rowIndex++)
            {
                var row = zoneData.Rows[rowIndex];
                string expectedStreetName = row["StreetName"]?.ToString()?.Trim() ?? "";
                string zone = row["Zone"]?.ToString()?.Trim() ?? "";

                if (string.IsNullOrEmpty(zone))
                {
                    Console.WriteLine($"Row {rowIndex + 1}: Empty zone, skipping...");
                    continue;
                }

                Console.WriteLine($"\n{'='} Testing Row {rowIndex + 1}/{totalZones}: Zone {zone} {'='}");
                Console.WriteLine($"Expected Street: {expectedStreetName}");

                report.CreateTest($"Zone Query {rowIndex + 1}: {zone}");
                
                try
                {
                    // Step 1: Type zone into input box
                    Console.WriteLine($"[{rowIndex + 1}] Step 1: Typing zone '{zone}'...");
                    await parkingPage.TypeZoneAsync(zone);
                    
                    // Step 2: Click Send button
                    Console.WriteLine($"[{rowIndex + 1}] Step 2: Clicking Send button...");
                    await parkingPage.ClickSendButtonAsync();
                        
                        // Step 2.5: Verify the message was sent
                        Console.WriteLine($"[{rowIndex + 1}] Step 2.5: Verifying message was sent...");
                        bool messageSent = await parkingPage.VerifyMessageSentAsync(zone);
                        if (!messageSent)
                        {
                            Console.WriteLine("WARNING: Message may not have been sent! Retrying...");
                            await parkingPage.ClickSendButtonAsync();
                            await Task.Delay(2000);
                        }
                        
                        // Step 3: Get the response
                        Console.WriteLine($"[{rowIndex + 1}] Step 3: Waiting for and getting response...");
                        await Task.Delay(2000); // Wait for bot to respond
                        string response = await parkingPage.GetResponseTextAsync(previousResponse);
                        Console.WriteLine($"Response Received: {response}");
                        previousResponse = response; // Store for next iteration
                        
                        // Validate result
                        string validationResult = ValidateResponse(expectedStreetName, response);
                        Console.WriteLine($"Validation: {validationResult}");
                        
                        // Determine if match (check if validation contains "MATCH" but not "NO MATCH")
                        bool isMatch = validationResult.StartsWith("MATCH");
                        string excelResult = isMatch ? "Match" : "No Match";
                        
                        // Write simple result to Excel
                        excelReader.WriteResult(permitFilePath, rowIndex, excelResult);
                        
                        // Log detailed result to report
                        report.Log(isMatch ? "Pass" : "Info", $"Zone: {zone} | {validationResult} | Response: {response}");
                        
                        // Step 4: Click Yes button (for ALL zones including last)
                        Console.WriteLine($"[{rowIndex + 1}] Step 4: Clicking 'Yes' button...");
                        try
                        {
                            await parkingPage.ClickYesButtonAsync();
                            report.Log("Pass", "Clicked 'Yes' button successfully");
                            Console.WriteLine($"[{rowIndex + 1}] ✓ 'Yes' button clicked");
                            await Task.Delay(1500);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{rowIndex + 1}] Failed to click Yes button: {ex.Message}");
                            report.Log("Fail", $"Failed to click Yes button: {ex.Message}");
                        }
                        
                        // Step 5: Click Main Menu button (for ALL zones including last)
                        Console.WriteLine($"[{rowIndex + 1}] Step 5: Clicking Main Menu...");
                        try
                        {
                            await parkingPage.ClickMainCardOptionAsync();
                            report.Log("Pass", "Main Menu clicked");
                            Console.WriteLine($"[{rowIndex + 1}] ✓ Main Menu clicked");
                            await Task.Delay(1500);
                        }
                        catch (Exception menuEx)
                        {
                            Console.WriteLine($"[{rowIndex + 1}] Warning: Main Menu click had issues: {menuEx.Message}");
                            report.Log("Info", "Main Menu click attempted");
                        }
                        
                        // Step 6: Different actions based on whether this is the last zone
                        if (rowIndex < totalZones - 1)
                        {
                            // Not the last zone - click Check Parking Streets again for next zone
                            Console.WriteLine($"[{rowIndex + 1}] Step 6: Clicking 'Check Parking Streets' for next zone...");
                            try
                            {
                                await parkingPage.OpenCardAsync();
                                await Task.Delay(2000);
                            }
                            catch (Exception cardEx)
                            {
                                Console.WriteLine($"Warning: Check Parking Streets click failed: {cardEx.Message}");
                            }
                            
                            // Wait for bot prompt for next zone
                            try
                            {
                                await baseClass.Page.WaitForSelectorAsync("text=Great! Please provide your zone.", new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15000 });
                                Console.WriteLine("Bot prompted for next zone");
                                await Task.Delay(1000);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Bot prompt not detected - {ex.Message}");
                                await Task.Delay(2000);
                            }
                        }
                        else
                        {
                            // This IS the last zone
                            Console.WriteLine($"\n{'='} LAST ZONE COMPLETED {'='}");
                            Console.WriteLine($"[{rowIndex + 1}] Step 6: Last zone - Yes and Main Menu already clicked");
                            Console.WriteLine($"[{rowIndex + 1}] Step 7: Waiting 20 seconds for UI to stabilize before FindCorrectZoneTest...");
                            await Task.Delay(20000);
                            Console.WriteLine("✓ 20 second wait completed - Find Correct Zone should now be visible");
                            Console.WriteLine("✓ Ready for FindCorrectZoneTest to start");
                            Console.WriteLine($"{'='} READY TO TRANSITION TO FIND CORRECT ZONE {'='}");
                        }
                        
                        Console.WriteLine($"\n✅ Zone {zone} test completed!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error testing Zone {zone}: {ex.Message}");
                        report.Log("Fail", $"Zone {zone}: {ex.Message}");
                        // Write simple ERROR to Excel, not the full error message
                        excelReader.WriteResult(permitFilePath, rowIndex, "ERROR");
                    }
                }

                Console.WriteLine($"\n======== All {totalZones} zones tested! ========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wokinhampermit tests: {ex.Message}");
                throw;
            }
        }
    }
}
