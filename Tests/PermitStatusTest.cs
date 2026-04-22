using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightAutomation.Base;
using PlaywrightAutomation.Pages;
using PlaywrightAutomation.Reporting;
using PlaywrightAutomation.Utils;

namespace PlaywrightAutomation.Tests
{
    public class PermitStatusTest
    {
        // ── Field extraction from bot response ───────────────────────────────

        /// <summary>
        /// Extracts a named field value from the bot response text.
        /// Uses the LAST occurrence so that accumulated chat history doesn't
        /// cause stale data from a previous row to be returned.
        /// e.g. ExtractField(response, "Current Status") -> "Cancelled"
        /// </summary>
        private static string ExtractField(string botResponse, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(botResponse)) return string.Empty;

            // Stop at: comma, newline, OR the start of the next field name
            // (2-4 Title-Case words immediately followed by a colon)
            // e.g. "Permit Expiry Date:" or "Status Description:"
            var pattern = Regex.Escape(fieldName)
                + @"\s*:\s*(.+?)(?=,|\n|[A-Z][a-zA-Z]+(?:\s+[A-Z][a-zA-Z]+){1,3}\s*:|$)";
            var matches = Regex.Matches(botResponse, pattern,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            // Use the LAST match to get the most recent bot response (avoids stale history)
            if (matches.Count == 0) return string.Empty;
            return NormalizeValue(matches[matches.Count - 1].Groups[1].Value);
        }

        /// <summary>
        /// Normalises a field value for comparison:
        /// strips surrounding whitespace/punctuation, collapses internal spaces,
        /// and unifies date separators so "31-10-2026" == "31/10/2026".
        /// </summary>
        private static string NormalizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var v = value.Trim().TrimEnd(',', '.', ';').Trim();
            // Unify date separators
            v = Regex.Replace(v, @"[\-/]", "-");
            // Collapse multiple spaces
            v = Regex.Replace(v, @"\s+", " ");
            return v;
        }

        // ── Field-level validation ────────────────────────────────────────────

        private static bool FieldMatches(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected)) return true;   // not provided — skip
            // If the bot response didn't return the field at all, log it but do NOT
            // auto-fail — return false so the caller can still report the mismatch clearly.
            if (string.IsNullOrWhiteSpace(actual))   return false;

            var exp = NormalizeValue(expected);
            var act = NormalizeValue(actual);

            // Accept if actual contains expected or expected contains actual
            return act.Contains(exp, StringComparison.OrdinalIgnoreCase) ||
                   exp.Contains(act, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns "Match" if ALL non-empty expected fields matched their extracted values.
        /// </summary>
        private static string OverallResult(string botResponse,
            string expCurrentStatus, string expCategory,
            string expExpiryDate,   string expDescription)
        {
            bool allMatch = true;

            if (!string.IsNullOrWhiteSpace(expCurrentStatus))
            {
                var actual = ExtractField(botResponse, "Current Status");
                var ok = FieldMatches(expCurrentStatus, actual);
                if (!ok) allMatch = false;
                string reason = string.IsNullOrWhiteSpace(actual) ? " (field not found in response)" : string.Empty;
                Console.WriteLine($"    Current Status  → expected: '{expCurrentStatus}' | actual: '{actual}' | {(ok ? "Match" : "No Match")}{reason}");
            }

            if (!string.IsNullOrWhiteSpace(expCategory))
            {
                var actual = ExtractField(botResponse, "Permit Category");
                var ok = FieldMatches(expCategory, actual);
                if (!ok) allMatch = false;
                string reason = string.IsNullOrWhiteSpace(actual) ? " (field not found in response)" : string.Empty;
                Console.WriteLine($"    Permit Category → expected: '{expCategory}' | actual: '{actual}' | {(ok ? "Match" : "No Match")}{reason}");
            }

            if (!string.IsNullOrWhiteSpace(expExpiryDate))
            {
                var actual = ExtractField(botResponse, "Permit Expiry Date");
                var ok = FieldMatches(expExpiryDate, actual);
                if (!ok) allMatch = false;
                string reason = string.IsNullOrWhiteSpace(actual) ? " (field not found in response)" : string.Empty;
                Console.WriteLine($"    Permit Expiry   → expected: '{expExpiryDate}' | actual: '{actual}' | {(ok ? "Match" : "No Match")}{reason}");
            }

            if (!string.IsNullOrWhiteSpace(expDescription))
            {
                var actual = ExtractField(botResponse, "Status Description");
                var ok = FieldMatches(expDescription, actual);
                if (!ok) allMatch = false;
                string reason = string.IsNullOrWhiteSpace(actual) ? " (field not found in response)" : string.Empty;
                Console.WriteLine($"    Status Desc     → expected: '{expDescription}' | actual: '{actual}' | {(ok ? "Match" : "No Match")}{reason}");
            }

            return allMatch ? "Match" : "No Match";
        }

        // ── Main entry point ──────────────────────────────────────────────────

        public async Task RunPermitStatusFlowAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("\n======== Starting Permit Status Flow Test ========");
            report.SetCategory("Permit Status Tests");
            Directory.CreateDirectory("Reporting");

            var configManager = ConfigManager.Instance;
            var excelReader      = new ExcelReader();
            var permitStatusPage = new PermitStatus(baseClass.Page);

            // Get dynamic status file path from config
            string filePath = configManager.GetFilePath("status");

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"ℹ️ {filePath} not found. Skipping Permit Status tests.");
                    return;
                }

                var testData = excelReader.ReadPermitStatusExcel(filePath);

                if (testData.Rows.Count == 0)
                {
                    Console.WriteLine("ℹ️ No rows found in PermitStatus sheet. Skipping.");
                    return;
                }

                // ── Pre-loop: click Yes to reach main menu cards ──────────────
                Console.WriteLine("  Pre-loop: clicking Yes to reach main cards");
                try { await permitStatusPage.ClickYesButtonAsync(); } catch { }
                await Task.Delay(1500);

                for (int rowIndex = 0; rowIndex < testData.Rows.Count; rowIndex++)
                {
                    DataRow row         = testData.Rows[rowIndex];
                    string lastName     = row["LastName"]?.ToString()?.Trim()         ?? string.Empty;
                    string postCode     = row["PostCode"]?.ToString()?.Trim()         ?? string.Empty;
                    string permitNumber = row["PermitNumber"]?.ToString()?.Trim()     ?? string.Empty;
                    string expStatus    = row["CurrentStatus"]?.ToString()?.Trim()    ?? string.Empty;
                    string expCategory  = row["PermitCategory"]?.ToString()?.Trim()   ?? string.Empty;
                    string expExpiry    = row["PermitExpiryDate"]?.ToString()?.Trim() ?? string.Empty;
                    string expDesc      = row["StatusDescription"]?.ToString()?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(lastName) &&
                        string.IsNullOrWhiteSpace(postCode) &&
                        string.IsNullOrWhiteSpace(permitNumber))
                    {
                        Console.WriteLine($"Row {rowIndex + 1}: All input fields empty, stopping.");
                        break;
                    }

                    report.CreateTest($"Permit Status Row {rowIndex + 1} – {permitNumber}");
                    Console.WriteLine($"\n[{rowIndex + 1}] LastName: '{lastName}' | PostCode: '{postCode}' | PermitNumber: '{permitNumber}'");

                    try
                    {
                        // ── Step 1: click Check Permit Status card ────────────────
                        await permitStatusPage.ClickCheckPermitStatusCardAsync();
                        await Task.Delay(2000);

                        // ── Step 2: clear all fields first, then fill ──────────────
                        int formIndex = rowIndex + 1;
                        await permitStatusPage.ClearFormFieldsAsync(formIndex);
                        await permitStatusPage.EnterLastNameAsync(lastName, formIndex);
                        await Task.Delay(500);
                        await permitStatusPage.EnterPostCodeAsync(postCode, formIndex);
                        await Task.Delay(500);
                        await permitStatusPage.EnterPermitNumberAsync(permitNumber, formIndex);
                        await Task.Delay(500);

                        // ── Step 3: click SUBMIT ──────────────────────────────────
                        await permitStatusPage.ClickSubmitButtonAsync(formIndex);

                        // ── Step 4: wait 5 seconds, then capture response ─────────
                        Console.WriteLine("  Waiting 5s for bot response...");
                        await Task.Delay(5000);
                        string botResponse = await permitStatusPage.GetPermitStatusResponseAsync();

                        if (string.IsNullOrWhiteSpace(botResponse))
                            Console.WriteLine($"  WARNING: No response captured for PermitNumber='{permitNumber}'");
                        else
                            Console.WriteLine($"  Captured: {(botResponse.Length > 120 ? botResponse[..120] + "..." : botResponse)}");

                        // ── Step 5: field-by-field validation ─────────────────────
                        Console.WriteLine("  Validating fields:");                        Console.WriteLine($"  [RAW RESPONSE]: {(botResponse.Length > 500 ? botResponse[..500] + "..." : botResponse)}");                        string result = OverallResult(botResponse, expStatus, expCategory, expExpiry, expDesc);

                        // ── Step 6: write actual response + result to Excel ───────
                        excelReader.WritePermitStatusRow(filePath, rowIndex, botResponse, result);
                        Console.WriteLine($"  Overall Result: {result}");

                        if (string.Equals(result, "Match", StringComparison.OrdinalIgnoreCase))
                            report.Log("Pass", $"PermitNumber: {permitNumber} | Result: {result}");
                        else
                            report.Log("Info", $"PermitNumber: {permitNumber} | Result: {result}");

                        // ── Step 7: click Yes to reset for next row ───────────────
                        Console.WriteLine("  Resetting: clicking Yes");
                        try { await permitStatusPage.ClickYesButtonAsync(); } catch { }
                        await Task.Delay(1500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR: {ex.Message}");
                        try { excelReader.WritePermitStatusRow(filePath, rowIndex, "", "No Match"); } catch { }
                        report.Log("Fail", $"PermitNumber: {permitNumber} | Error: {ex.Message}");

                        // Recovery: try Yes then main menu
                        try { await permitStatusPage.ClickYesButtonAsync(); } catch { }
                        await Task.Delay(1000);
                        try { await permitStatusPage.ClickMainMenuAsync(); } catch { }
                        await Task.Delay(1000);
                    }
                }

                Console.WriteLine("======== Completed Permit Status Flow Test ========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Permit Status flow tests: {ex.Message}");
                throw;
            }
        }
    }
}
