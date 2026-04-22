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
    public class GeneralQueriesTest
    {
        private string ValidateResponse(string expectedResponse, string actualResponse)
        {
            if (string.IsNullOrWhiteSpace(expectedResponse) || string.IsNullOrWhiteSpace(actualResponse))
                return "No Match";

            var normalizedExpected = NormalizeText(expectedResponse);
            var normalizedActual = NormalizeText(actualResponse);

            if (normalizedActual.Contains(normalizedExpected) || normalizedExpected.Contains(normalizedActual))
                return "Match";

            var similarity = CalculateSimilarity(normalizedExpected, normalizedActual);
            Console.WriteLine($"  Similarity: {similarity:P1}");
            if (similarity >= 0.55)
                return "Match";

            return "No Match";
        }

        private double CalculateSimilarity(string expected, string actual)
        {
            var expectedTokens = Tokenize(expected);
            var actualTokens = Tokenize(actual);

            if (expectedTokens.Count == 0 || actualTokens.Count == 0)
                return 0;

            int overlap = expectedTokens.Intersect(actualTokens).Count();
            return (double)overlap / expectedTokens.Count;
        }

        private HashSet<string> Tokenize(string text)
        {
            return text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length > 2)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.ToLowerInvariant()
                .Replace("\r", " ")
                .Replace("\n", " ");

            normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s]", " ");
            normalized = Regex.Replace(normalized, @"\b(a|an|the)\b", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        public async Task RunGeneralQueriesFlowAsync(BaseClass baseClass, ExtentReport report)
        {
            Console.WriteLine("\n======== Starting General Queries Flow Test ========");
            report.SetCategory("General Queries");
            Directory.CreateDirectory("Reporting");

            var configManager = ConfigManager.Instance;
            var excelReader = new ExcelReader();
            var generalQueriesPage = new GeneralQueris(baseClass.Page);

            // Get dynamic queries file path from config
            string generalQueriesFilePath = configManager.GetFilePath("queries");

            try
            {
                var queryData = excelReader.ReadGeneralQueriesExcel(generalQueriesFilePath);

                // Ensure we are on main cards and open General Queries
                try
                {
                    await generalQueriesPage.ClickYesButtonAsync();
                }
                catch
                {
                    // Ignore if Yes is not present
                }

                try
                {
                    await generalQueriesPage.ClickMainCardOptionAsync();
                }
                catch
                {
                    // Ignore if already on main cards
                }

                await generalQueriesPage.OpenCardAsync();

                for (int rowIndex = 0; rowIndex < queryData.Rows.Count; rowIndex++)
                {
                    DataRow row = queryData.Rows[rowIndex];
                    string question = row["Question"]?.ToString()?.Trim() ?? string.Empty;
                    string expectedResponse = row["ExpectedResponse"]?.ToString()?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(question))
                    {
                        Console.WriteLine($"Row {rowIndex + 1}: Empty question found, stopping.");
                        break;
                    }

                    report.CreateTest($"General Query {rowIndex + 1}");

                    try
                    {
                        // Step 1: Snapshot all existing message texts before sending
                        var textSnapshot = await generalQueriesPage.GetBotMessageTextsSnapshotAsync();

                        // Step 2: Type question and send
                        Console.WriteLine($"[{rowIndex + 1}] Asking: {question}");
                        await generalQueriesPage.TypeQuestionAsync(question);
                        await generalQueriesPage.ClickSendButtonAsync();

                        // Step 3: Small delay to let bot start responding, then wait for real answer
                        await Task.Delay(3000);
                        string botResponse = await generalQueriesPage.GetResponseTextAsync(textSnapshot);
                        Console.WriteLine($"  Captured: {(botResponse.Length > 80 ? botResponse.Substring(0, 80) + "..." : botResponse)}");

                        // Step 3: Write actual bot response and result to Excel
                        excelReader.WriteActualBotResponse(generalQueriesFilePath, rowIndex, botResponse);

                        string result = ValidateResponse(expectedResponse, botResponse);
                        excelReader.WriteResult(generalQueriesFilePath, rowIndex, result);

                        if (string.Equals(result, "Match", StringComparison.OrdinalIgnoreCase))
                        {
                            report.Log("Pass", $"Question: {question} | Result: {result}");
                        }
                        else
                        {
                            report.Log("Info", $"Question: {question} | Result: {result}");
                        }

                        // Step 4: Reset bot context — Yes → Main Menu → General Queries
                        if (rowIndex < queryData.Rows.Count - 1)
                        {
                            Console.WriteLine($"  Resetting: Yes → Main Menu → General Queries");

                            try { await generalQueriesPage.ClickYesButtonAsync(); }
                            catch { /* Yes button may not appear */ }

                            try { await generalQueriesPage.ClickMainCardOptionAsync(); }
                            catch { /* May already be at main menu */ }

                            await generalQueriesPage.OpenCardAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        excelReader.WriteResult(generalQueriesFilePath, rowIndex, "No Match");
                        report.Log("Fail", $"Question: {question} | Error: {ex.Message}");

                        // Try to recover for next question
                        try
                        {
                            try { await generalQueriesPage.ClickYesButtonAsync(); } catch { }
                            try { await generalQueriesPage.ClickMainCardOptionAsync(); } catch { }
                            await generalQueriesPage.OpenCardAsync();
                        }
                        catch { /* Best effort recovery */ }
                    }
                }

                Console.WriteLine("======== Completed General Queries Flow Test ========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in General Queries flow tests: {ex.Message}");
                throw;
            }
        }
    }
}