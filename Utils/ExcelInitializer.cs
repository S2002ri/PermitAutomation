using OfficeOpenXml;
using System.IO;

namespace PlaywrightAutomation.Utils
{
    public class ExcelInitializer
    {
        public static void InitializeTestDataFile(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("TestData");
                
                // Add headers
                worksheet.Cells[1, 1].Value = "FirstName";
                worksheet.Cells[1, 2].Value = "LastName";
                worksheet.Cells[1, 3].Value = "MobileNo";
                worksheet.Cells[1, 4].Value = "LoginUrl";
                worksheet.Cells[1, 5].Value = "ChatInputBox";

                // Add data
                worksheet.Cells[2, 1].Value = "Saranya";
                worksheet.Cells[2, 2].Value = "Eswaran";
                worksheet.Cells[2, 3].Value = "8072424475";
                worksheet.Cells[2, 4].Value = "https://mia.permit.marstonholdings.co.uk/C1CA422B-A4CC-4B26-B9FD-4280CD69E743";
                worksheet.Cells[2, 5].Value = "w1";

                package.Save();
            }
        }

        public static void InitializeTestCasesFile(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("TestCases");
                
                // Add headers
                worksheet.Cells[1, 1].Value = "TestCaseID";
                worksheet.Cells[1, 2].Value = "Test Case";
                worksheet.Cells[1, 3].Value = "Status";

                // Add test cases
                string[,] testCases = {
                    {"96937", "Check whether the Permit bot URL gets loaded", ""},
                    {"110841", "Verify that the chatbot displays a greeting message upon opening.", ""},
                    {"110842", "Verify chatbot prompt and adaptive card form with fields", ""},
                    {"110843", "Verify first name field accepts only alphabets, sanitizes numeric, max 25 chars", ""},
                    {"110844", "Verify last name field accepts only alphabets, sanitizes numeric, max 25 chars", ""},
                    {"110845", "Verify mobile number field accepts only numbers, sanitizes non-numeric, max 11 digits", ""},
                    {"110846", "Verify submit button enabled only when all fields are filled, disabled otherwise", ""},
                    {"110847", "Verify submitting valid form leads to main card options", ""}
                };

                for (int i = 0; i < testCases.GetLength(0); i++)
                {
                    worksheet.Cells[i + 2, 1].Value = testCases[i, 0];
                    worksheet.Cells[i + 2, 2].Value = testCases[i, 1];
                    worksheet.Cells[i + 2, 3].Value = testCases[i, 2];
                }

                package.Save();
            }
        }

        public static void InitializeWokinghampermitFile(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("Zones");
                
                // Add headers
                worksheet.Cells[1, 1].Value = "StreetName";
                worksheet.Cells[1, 2].Value = "Zone";
                worksheet.Cells[1, 3].Value = "Result";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 3])
                {
                    range.Style.Font.Bold = true;
                }

                // Add sample zone data (modify as needed)
                worksheet.Cells[2, 2].Value = "W1";
                worksheet.Cells[3, 2].Value = "W2";
                worksheet.Cells[4, 2].Value = "W3";

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                package.Save();
            }
        }
    }
}