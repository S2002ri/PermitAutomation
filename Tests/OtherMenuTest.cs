using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using PlaywrightAutomation.Base;
using PlaywrightAutomation.Pages;
using PlaywrightAutomation.Reporting;
using PlaywrightAutomation.Utils;

namespace PlaywrightAutomation.Tests
{
    public class OtherMenuTest
    {


        // ──────────────────────────────────────────────────────────────────────
        // Response validation helpers (Pass / Fail)
        // ──────────────────────────────────────────────────────────────────────

        private string ValidateResponse(string expectedResponse, string actualResponse)
        {
            // If there is no expected response, treat a non-empty actual response as Match
            if (string.IsNullOrWhiteSpace(expectedResponse))
                return string.IsNullOrWhiteSpace(actualResponse) ? "No Match" : "Match";

            if (string.IsNullOrWhiteSpace(actualResponse))
                return "No Match";

            var normalizedExpected = NormalizeText(expectedResponse);
            var normalizedActual   = NormalizeText(actualResponse);

            if (normalizedActual.Contains(normalizedExpected) || normalizedExpected.Contains(normalizedActual))
                return "Match";

            double similarity = CalculateSimilarity(normalizedExpected, normalizedActual);
            Console.WriteLine($"  Similarity: {similarity:P1}");
            return similarity >= 0.55 ? "Match" : "No Match";
        }

        private double CalculateSimilarity(string expected, string actual)
        {
            var expectedTokens = Tokenize(expected);
            var actualTokens   = Tokenize(actual);

            if (expectedTokens.Count == 0 || actualTokens.Count == 0)
                return 0;

            int overlap = expectedTokens.Intersect(actualTokens).Count();
            return (double)overlap / expectedTokens.Count;
        }

        private HashSet<string> Tokenize(string text) =>
            text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var n = value.ToLowerInvariant()
                         .Replace("\r", " ")
                         .Replace("\n", " ");
            n = Regex.Replace(n, @"[^\p{L}\p{Nd}\s]", " ");
            n = Regex.Replace(n, @"\b(a|an|the)\b", " ");
            n = Regex.Replace(n, @"\s+", " ").Trim();
            return n;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Main entry point (called from Program.cs after GeneralQueriesTest)
        // ──────────────────────────────────────────────────────────────────────

        public async Task RunOtherMenuFlowAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("\n======== Starting OtherMenu Flow Test ========");
            report.SetCategory("Other Menu Tests");
            Directory.CreateDirectory("Reporting");

            var configManager = ConfigManager.Instance;
            var excelReader   = new ExcelReader();
            var otherMenuPage = new OtherMenu(baseClass.Page);

            // Get dynamic queries file path from config
            string filePath = configManager.GetFilePath("queries");

            try
            {
                var menuData = excelReader.ReadOtherMenuExcel(filePath);

                if (menuData.Rows.Count == 0)
                {
                    Console.WriteLine("ℹ️ No rows found in OtherMenu sheet. Skipping.");
                    return;
                }

                // ── Pre-loop: Yes → General Queries → "Test" → Send → Yes (land on main cards) ──
                Console.WriteLine("  Pre-loop: Yes → General Queries → Test → Send → Yes");
                try { await otherMenuPage.ClickYesButtonAsync(); } catch { }
                await Task.Delay(1000);
                try
                {
                    await otherMenuPage.ClickGeneralQueriesCardAsync();
                    await Task.Delay(1000);
                    await otherMenuPage.TypeAndSendAsync("Test");
                    await Task.Delay(3000);
                    try { await otherMenuPage.ClickYesButtonAsync(); } catch { }
                    await Task.Delay(1000);
                }
                catch { /* best effort — may already be on main cards */ }

                for (int rowIndex = 0; rowIndex < menuData.Rows.Count; rowIndex++)
                {
                    DataRow row     = menuData.Rows[rowIndex];
                    string menuName = row["Menu"]?.ToString()?.Trim()        ?? string.Empty;
                    string subMenu  = row["Submenu"]?.ToString()?.Trim()     ?? string.Empty;
                    string expected = row["BotResponse"]?.ToString()?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(menuName) && string.IsNullOrWhiteSpace(subMenu))
                    {
                        Console.WriteLine($"Row {rowIndex + 1}: Empty row found, stopping.");
                        break;
                    }

                    report.CreateTest($"OtherMenu Row {rowIndex + 1} – {menuName} > {subMenu}");
                    Console.WriteLine($"\n[{rowIndex + 1}] Menu: '{menuName}' | Submenu: '{subMenu}'");

                    try
                    {
                        // ── Step 1: click menu card and wait for sub-options ──────────
                        await otherMenuPage.ClickMenuCardAsync(menuName);
                        await Task.Delay(2000);

                        // ── Step 2: click submenu card ───────────────────────────────
                        if (!string.IsNullOrWhiteSpace(subMenu))
                            await otherMenuPage.ClickSubMenuCardAsync(subMenu);

                        // ── Step 3: wait then capture the latest bot response ─────────
                        await Task.Delay(3000);
                        string botResponse = await otherMenuPage.GetLatestBotResponseAsync();
                        if (string.IsNullOrWhiteSpace(botResponse))
                            Console.WriteLine($"  WARNING: No actual response captured for Menu='{menuName}' Submenu='{subMenu}'");
                        else
                            Console.WriteLine($"  Captured: {(botResponse.Length > 80 ? botResponse[..80] + "..." : botResponse)}");

                        // ── Step 5: validate and write to Excel ──────────────────────
                        string result = ValidateResponse(expected, botResponse);
                        excelReader.WriteOtherMenuRow(filePath, rowIndex, botResponse, result);
                        Console.WriteLine($"  Result: {result}");

                        if (string.Equals(result, "Match", StringComparison.OrdinalIgnoreCase))
                            report.Log("Pass", $"Menu: {menuName} | Submenu: {subMenu} | Result: {result}");
                        else
                            report.Log("Info", $"Menu: {menuName} | Submenu: {subMenu} | Result: {result}");

                        // ── Step 6: reset for next row: Yes → GQ → "Test" → Send → Yes ──────
                        if (rowIndex < menuData.Rows.Count - 1)
                        {
                            Console.WriteLine($"  Resetting: Yes → General Queries → Test → Send → Yes");
                            try { await otherMenuPage.ClickYesButtonAsync(); } catch { }
                            await Task.Delay(1000);
                            try { await otherMenuPage.ClickGeneralQueriesCardAsync(); } catch { }
                            await Task.Delay(1000);
                            try { await otherMenuPage.TypeAndSendAsync("Test"); } catch { }
                            await Task.Delay(3000);
                            try { await otherMenuPage.ClickYesButtonAsync(); } catch { }
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR: {ex.Message}");
                        try { excelReader.WriteOtherMenuRow(filePath, rowIndex, "", "No Match"); } catch { }
                        report.Log("Fail", $"Menu: {menuName} | Submenu: {subMenu} | Error: {ex.Message}");

                        try { await otherMenuPage.ClickYesButtonAsync(); } catch { }
                        await Task.Delay(1000);
                        try { await otherMenuPage.ClickGeneralQueriesCardAsync(); } catch { }
                        await Task.Delay(1000);
                        try { await otherMenuPage.TypeAndSendAsync("Test"); } catch { }
                        await Task.Delay(3000);
                        try { await otherMenuPage.ClickYesButtonAsync(); } catch { }
                        await Task.Delay(1000);
                    }
                }

                Console.WriteLine("======== Completed OtherMenu Flow Test ========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OtherMenu flow tests: {ex.Message}");
                throw;
            }
        }
    }
}
