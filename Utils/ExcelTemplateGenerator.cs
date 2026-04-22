using OfficeOpenXml;
using System;
using System.IO;
using PlaywrightAutomation.Utils;

namespace PlaywrightAutomation.Utils
{
    /// <summary>
    /// Generates Excel template files for all councils based on Wokingham templates
    /// </summary>
    public class ExcelTemplateGenerator
    {
        /// <summary>
        /// Creates all Excel template files for a new council
        /// </summary>
        public static void CreateCouncilTemplates(string councilName)
        {
            Console.WriteLine($"Creating Excel templates for {councilName}...");

            var configManager = ConfigManager.Instance;
            var council = configManager.GetAllCouncils().Find(c =>
                c.Name.Equals(councilName, StringComparison.OrdinalIgnoreCase));

            if (council == null)
            {
                Console.WriteLine($"Council '{councilName}' not found in config.");
                return;
            }

            // Create TestData file
            CreateTestDataTemplate(council.Files.TestData);

            // Create TestCases file
            CreateTestCasesTemplate(council.Files.TestCases);

            // Create Permit file
            CreatePermitTemplate(council.Files.Permit);

            // Create Zones file
            CreateZonesTemplate(council.Files.Zones);

            // Create Queries file
            CreateQueriesTemplate(council.Files.Queries);

            // Create Status file
            CreateStatusTemplate(council.Files.Status);

            Console.WriteLine($"All templates created successfully for {councilName}!");
        }

        /// <summary>
        /// Creates all templates for all councils in config
        /// </summary>
        public static void CreateAllCouncilTemplates()
        {
            Console.WriteLine("Creating Excel templates for all councils...");

            var configManager = ConfigManager.Instance;
            var councils = configManager.GetAllCouncils();

            foreach (var council in councils)
            {
                // Skip if files already exist (keep Wokingham as is)
                if (File.Exists(council.Files.TestData) &&
                    council.Name.Equals("Wokingham", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Skipping {council.Name} - files already exist.");
                    continue;
                }

                CreateCouncilTemplates(council.Name);
            }

            Console.WriteLine("All council templates created!");
        }

        private static void CreateTestDataTemplate(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  TestData file already exists: {filePath}");
                return;
            }

            EnsureDirectoryExists(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("TestData");

                // Headers
                worksheet.Cells[1, 1].Value = "FirstName";
                worksheet.Cells[1, 2].Value = "LastName";
                worksheet.Cells[1, 3].Value = "MobileNo";
                worksheet.Cells[1, 4].Value = "LoginUrl";
                worksheet.Cells[1, 5].Value = "ChatInputBox";

                // Data - Use LOGIN_URL placeholder, will be replaced by config
                worksheet.Cells[2, 1].Value = "Maximiliano";
                worksheet.Cells[2, 2].Value = "Eswaran";
                worksheet.Cells[2, 3].Value = "8072424475";
                worksheet.Cells[2, 4].Value = "LOGIN_URL";  // Will be replaced by config
                worksheet.Cells[2, 5].Value = "w1";

                package.Save();
                Console.WriteLine($"  Created: {filePath}");
            }
        }

        private static void CreateTestCasesTemplate(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  TestCases file already exists: {filePath}");
                return;
            }

            EnsureDirectoryExists(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("TestCases");

                // Headers
                worksheet.Cells[1, 1].Value = "TestCaseID";
                worksheet.Cells[1, 2].Value = "Test Case";
                worksheet.Cells[1, 3].Value = "Status";

                // Test Cases
                string[,] testCases = {
                    {"96937", "Check whether the Permit bot URL gets loaded", ""},
                    {"110841", "Verify that the chatbot displays a greeting message upon opening.", ""},
                    {"110842", "Verify chatbot prompt and adaptive card form with fields", ""},
                    {"110843", "Verify first name field accepts only alphabets, sanitizes numeric, max 25 chars", ""},
                    {"110844", "Verify last name field accepts only alphabets, sanitizes numeric, max 25 chars", ""},
                    {"110845", "Verify mobile number field accepts only numbers, sanitizes non-numeric, max 11 digits", ""},
                    {"110846", "Verify submit button enabled only when all fields are filled, disabled otherwise", ""},
                    {"110847", "Verify the user able to enter Zone and Validate the responses", ""},
                    {"110848", "Verify the user able to click on Check Parking Street card", ""},
                    {"110849", "Verify the user able to enter Zone and see the response", ""},
                    {"110850", "Verify the user able to enter Post Code and find matching Zones", ""},
                    {"110851", "Verify the user able to click on Find Correct Zone card", ""},
                    {"110852", "Verify the user able to enter the Post Code and find the matching Zones", ""}
                };

                for (int i = 0; i < testCases.GetLength(0); i++)
                {
                    worksheet.Cells[i + 2, 1].Value = testCases[i, 0];
                    worksheet.Cells[i + 2, 2].Value = testCases[i, 1];
                    worksheet.Cells[i + 2, 3].Value = testCases[i, 2];
                }

                package.Save();
                Console.WriteLine($"  Created: {filePath}");
            }
        }

        private static void CreatePermitTemplate(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  Permit file already exists: {filePath}");
                return;
            }

            EnsureDirectoryExists(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("Zones");

                // Headers
                worksheet.Cells[1, 1].Value = "StreetName";
                worksheet.Cells[1, 2].Value = "Zone";
                worksheet.Cells[1, 3].Value = "Result";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 3])
                {
                    range.Style.Font.Bold = true;
                }

                // Sample data rows (empty for template)
                worksheet.Cells[2, 1].Value = "";
                worksheet.Cells[2, 2].Value = "W1";
                worksheet.Cells[2, 3].Value = "";

                worksheet.Cells[3, 1].Value = "";
                worksheet.Cells[3, 2].Value = "W2";
                worksheet.Cells[3, 3].Value = "";

                worksheet.Cells[4, 1].Value = "";
                worksheet.Cells[4, 2].Value = "W3";
                worksheet.Cells[4, 3].Value = "";

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                package.Save();
                Console.WriteLine($"  Created: {filePath}");
            }
        }

        private static void CreateZonesTemplate(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  Zones file already exists: {filePath}");
                return;
            }

            EnsureDirectoryExists(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("Zones");

                // Headers
                worksheet.Cells[1, 1].Value = "Post Code";
                worksheet.Cells[1, 2].Value = "Zone";
                worksheet.Cells[1, 3].Value = "Result";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 3])
                {
                    range.Style.Font.Bold = true;
                }

                // Sample data rows (empty for template)
                worksheet.Cells[2, 1].Value = "RG40 1GA";
                worksheet.Cells[2, 2].Value = "W1";
                worksheet.Cells[2, 3].Value = "";

                worksheet.Cells[3, 1].Value = "RG40 1GB";
                worksheet.Cells[3, 2].Value = "W2";
                worksheet.Cells[3, 3].Value = "";

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                package.Save();
                Console.WriteLine($"  Created: {filePath}");
            }
        }

        private static void CreateQueriesTemplate(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  Queries file already exists: {filePath}");
                return;
            }

            EnsureDirectoryExists(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Sheet 1: General Queries
                var worksheet1 = package.Workbook.Worksheets.Add("Queries");

                // Headers
                worksheet1.Cells[1, 1].Value = "Question";
                worksheet1.Cells[1, 2].Value = "ExpectedResponse";
                worksheet1.Cells[1, 3].Value = "Result";

                // Format headers
                using (var range = worksheet1.Cells[1, 1, 1, 3])
                {
                    range.Style.Font.Bold = true;
                }

                // Sample data
                worksheet1.Cells[2, 1].Value = "How do I apply for a permit?";
                worksheet1.Cells[2, 2].Value = "";
                worksheet1.Cells[2, 3].Value = "";

                worksheet1.Cells[3, 1].Value = "What are the permit fees?";
                worksheet1.Cells[3, 2].Value = "";
                worksheet1.Cells[3, 3].Value = "";

                // Auto-fit columns
                worksheet1.Cells.AutoFitColumns();

                // Sheet 2: Menu (for OtherMenuTest)
                var worksheet2 = package.Workbook.Worksheets.Add("Menu");

                // Headers
                worksheet2.Cells[1, 1].Value = "Menu";
                worksheet2.Cells[1, 2].Value = "Submenu";
                worksheet2.Cells[1, 3].Value = "BotResponse";
                worksheet2.Cells[1, 4].Value = "Result";

                // Format headers
                using (var range = worksheet2.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                }

                // Sample data
                worksheet2.Cells[2, 1].Value = "Account Management";
                worksheet2.Cells[2, 2].Value = "Update Contact Details";
                worksheet2.Cells[2, 3].Value = "";
                worksheet2.Cells[2, 4].Value = "";

                worksheet2.Cells[3, 1].Value = "Payment & Fee";
                worksheet2.Cells[3, 2].Value = "Payment Options";
                worksheet2.Cells[3, 3].Value = "";
                worksheet2.Cells[3, 4].Value = "";

                // Auto-fit columns
                worksheet2.Cells.AutoFitColumns();

                package.Save();
                Console.WriteLine($"  Created: {filePath}");
            }
        }

        private static void CreateStatusTemplate(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"  Status file already exists: {filePath}");
                return;
            }

            EnsureDirectoryExists(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("PermitStatus");

                // Headers
                worksheet.Cells[1, 1].Value = "LastName";
                worksheet.Cells[1, 2].Value = "PostCode";
                worksheet.Cells[1, 3].Value = "PermitNumber";
                worksheet.Cells[1, 4].Value = "CurrentStatus";
                worksheet.Cells[1, 5].Value = "PermitCategory";
                worksheet.Cells[1, 6].Value = "PermitExpiryDate";
                worksheet.Cells[1, 7].Value = "StatusDescription";
                worksheet.Cells[1, 8].Value = "Result";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                }

                // Sample data rows (empty for template)
                for (int i = 2; i <= 5; i++)
                {
                    worksheet.Cells[i, 1].Value = "";
                    worksheet.Cells[i, 2].Value = "";
                    worksheet.Cells[i, 3].Value = "";
                    worksheet.Cells[i, 4].Value = "";
                    worksheet.Cells[i, 5].Value = "";
                    worksheet.Cells[i, 6].Value = "";
                    worksheet.Cells[i, 7].Value = "";
                    worksheet.Cells[i, 8].Value = "";
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                package.Save();
                Console.WriteLine($"  Created: {filePath}");
            }
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
