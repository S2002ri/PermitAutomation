using System;
using System.Data;
using System.IO;
using OfficeOpenXml;

namespace PlaywrightAutomation.Utils
{
    public class ExcelReader
    {
        public DataTable ReadExcel(string filePath)
        {
            try
            {
                Console.WriteLine($"Reading Excel file: {filePath}");
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Excel file not found at {filePath}");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var dataTable = new DataTable();

                    bool isTestCasesFile = filePath.Contains("TestCases");
                    string[] headers = isTestCasesFile
                        ? new[] { "TestCaseID", "Test Case", "Status" }
                        : new[] { "FirstName", "LastName", "MobileNo", "LoginUrl", "ChatInputBox" };

                    foreach (var header in headers)
                    {
                        dataTable.Columns.Add(header);
                    }

                    // Read data
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var dataRow = dataTable.NewRow();
                        if (!filePath.Contains("TestCases"))
                        {
                            // For TestData.xlsx, read from separate columns
                            dataRow["FirstName"] = worksheet.Cells[row, 1].Text;
                            dataRow["LastName"] = worksheet.Cells[row, 2].Text;
                            dataRow["MobileNo"] = worksheet.Cells[row, 3].Text;
                            dataRow["LoginUrl"] = worksheet.Cells[row, 4].Text;
                            dataRow["ChatInputBox"] = worksheet.Cells[row, 5].Text;
                            
                            Console.WriteLine($"Reading data row {row}:");
                            Console.WriteLine($"  FirstName: {dataRow["FirstName"]}");
                            Console.WriteLine($"  LastName: {dataRow["LastName"]}");
                            Console.WriteLine($"  MobileNo: {dataRow["MobileNo"]}");
                            Console.WriteLine($"  LoginUrl: {dataRow["LoginUrl"]}");
                            Console.WriteLine($"  ChatInputBox: {dataRow["ChatInputBox"]}");
                        }
                        else
                        {
                            // For TestCases.xlsx
                            if (worksheet.Cells[row, 1].Text != "")
                            {
                                Console.WriteLine($"Reading test case row {row}:");
                                dataRow["TestCaseID"] = worksheet.Cells[row, 1].Text;
                                dataRow["Test Case"] = worksheet.Cells[row, 2].Text;
                                dataRow["Status"] = worksheet.Cells[row, 3].Text;
                                Console.WriteLine($"  ID: {dataRow["TestCaseID"]}");
                                Console.WriteLine($"  Test: {dataRow["Test Case"]}");
                                Console.WriteLine($"  Status: {dataRow["Status"]}");
                            }
                        }
                        bool hasData = filePath.Contains("TestCases")
                            ? !string.IsNullOrEmpty(dataRow["TestCaseID"].ToString())
                            : !string.IsNullOrEmpty(dataRow["FirstName"].ToString());
                            
                        if (hasData)
                        {
                            dataTable.Rows.Add(dataRow);
                        }
                    }

                    Console.WriteLine($"Read {dataTable.Rows.Count} rows of data");
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Excel file: {ex.Message}");
                throw;
            }
        }

        public void WriteStatus(string filePath, string testCaseId, string status)
        {
            try
            {
                Console.WriteLine($"Writing status for test case {testCaseId}: {status}");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    bool found = false;

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        if (worksheet.Cells[row, 1].Text == testCaseId)
                        {
                            worksheet.Cells[row, 3].Value = status;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine($"Warning: Test case {testCaseId} not found in Excel file");
                    }
                    else
                    {
                        package.Save();
                        Console.WriteLine($"Status updated successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing status to Excel: {ex.Message}");
                throw;
            }
        }

        public DataTable ReadWokinhampermitExcel(string filePath)
        {
            try
            {
                Console.WriteLine($"📄 Reading Wokinhampermit Excel file: {filePath}");
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Excel file not found at {filePath}");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var dataTable = new DataTable();

                    dataTable.Columns.Add("StreetName");
                    dataTable.Columns.Add("Zone");
                    dataTable.Columns.Add("Result");

                    // Read data starting from row 2 (skip header)
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var streetName = worksheet.Cells[row, 1].Text.Trim();
                        var zone = worksheet.Cells[row, 2].Text.Trim();
                        var result = worksheet.Cells[row, 3].Text.Trim();

                        if (!string.IsNullOrEmpty(streetName) || !string.IsNullOrEmpty(zone))
                        {
                            var dataRow = dataTable.NewRow();
                            dataRow["StreetName"] = streetName;
                            dataRow["Zone"] = zone;
                            dataRow["Result"] = result;
                            dataTable.Rows.Add(dataRow);
                        }
                    }

                    Console.WriteLine($"✅ Loaded {dataTable.Rows.Count} rows from Wokinhampermit.xlsx");
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading Wokinhampermit Excel file: {ex.Message}");
                throw;
            }
        }

        public DataTable ReadResidentialZonesExcel(string filePath)
        {
            try
            {
                Console.WriteLine($"📄 Reading Residential Zones Excel file: {filePath}");
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Excel file not found at {filePath}");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var dataTable = new DataTable();

                    dataTable.Columns.Add("PostCode");
                    dataTable.Columns.Add("Zone");
                    dataTable.Columns.Add("Result");

                    // Detect columns from header row, fallback to A/B/C
                    int postCodeColumn = 1;
                    int zoneColumn = 2;
                    int resultColumn = 3;

                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerText = worksheet.Cells[1, col].Text.Trim();
                        if (string.Equals(headerText, "Post Code", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(headerText, "Postcode", StringComparison.OrdinalIgnoreCase))
                        {
                            postCodeColumn = col;
                        }
                        else if (string.Equals(headerText, "Zone", StringComparison.OrdinalIgnoreCase))
                        {
                            zoneColumn = col;
                        }
                        else if (string.Equals(headerText, "Result", StringComparison.OrdinalIgnoreCase))
                        {
                            resultColumn = col;
                        }
                    }

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var postCode = worksheet.Cells[row, postCodeColumn].Text.Trim();
                        var zone = worksheet.Cells[row, zoneColumn].Text.Trim();
                        var result = worksheet.Cells[row, resultColumn].Text.Trim();

                        if (string.IsNullOrWhiteSpace(postCode) && string.IsNullOrWhiteSpace(zone))
                        {
                            Console.WriteLine($"ℹ️ Blank row encountered at Excel row {row}. Stopping Residential Zones read loop.");
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace(postCode) || !string.IsNullOrWhiteSpace(zone))
                        {
                            var dataRow = dataTable.NewRow();
                            dataRow["PostCode"] = postCode;
                            dataRow["Zone"] = zone;
                            dataRow["Result"] = result;
                            dataTable.Rows.Add(dataRow);
                        }
                    }

                    Console.WriteLine($"✅ Loaded {dataTable.Rows.Count} rows from Residential Zones file");
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading Residential Zones Excel file: {ex.Message}");
                throw;
            }
        }

        public void WriteResult(string filePath, int rowIndex, string result)
        {
            try
            {
                Console.WriteLine($"🖊️ Writing result for row {rowIndex}: {result}");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    int resultColumn = 3;
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerText = worksheet.Cells[1, col].Text.Trim();
                        if (string.Equals(headerText, "Result", StringComparison.OrdinalIgnoreCase))
                        {
                            resultColumn = col;
                            break;
                        }
                    }

                    // rowIndex is 0-based in DataTable, but Excel rows are 1-based and row 1 is header
                    int excelRow = rowIndex + 2;
                    worksheet.Cells[excelRow, resultColumn].Value = result;
                    package.Save();
                    Console.WriteLine($"✅ Result written successfully to row {excelRow}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing result to Excel: {ex.Message}");
                throw;
            }
        }

        public void WriteActualBotResponse(string filePath, int rowIndex, string botResponse)
        {
            try
            {
                Console.WriteLine($"🖊️ Writing actual bot response for row {rowIndex}");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    int actualColumn = 0;
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerText = worksheet.Cells[1, col].Text.Trim();
                        if (headerText.Contains("actual", StringComparison.OrdinalIgnoreCase) &&
                            headerText.Contains("response", StringComparison.OrdinalIgnoreCase))
                        {
                            actualColumn = col;
                            break;
                        }
                    }

                    if (actualColumn == 0)
                    {
                        actualColumn = worksheet.Dimension.End.Column + 1;
                        worksheet.Cells[1, actualColumn].Value = "ActualBotResponse";
                    }

                    int excelRow = rowIndex + 2;
                    worksheet.Cells[excelRow, actualColumn].Value = botResponse;
                    package.Save();
                    Console.WriteLine($"✅ Actual bot response written successfully to row {excelRow}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing actual bot response: {ex.Message}");
                throw;
            }
        }

        public DataTable ReadGeneralQueriesExcel(string filePath)
        {
            try
            {
                Console.WriteLine($"📄 Reading General Queries Excel file: {filePath}");
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Excel file not found at {filePath}");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var dataTable = new DataTable();

                    dataTable.Columns.Add("Question");
                    dataTable.Columns.Add("ExpectedResponse");
                    dataTable.Columns.Add("Result");

                    int questionColumn = 1;
                    int expectedColumn = 2;
                    int resultColumn = 3;

                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerText = worksheet.Cells[1, col].Text.Trim();
                        if (string.IsNullOrWhiteSpace(headerText))
                            continue;

                        if (headerText.Contains("question", StringComparison.OrdinalIgnoreCase) ||
                            headerText.Contains("query", StringComparison.OrdinalIgnoreCase))
                        {
                            questionColumn = col;
                        }
                        else if (headerText.Contains("expected", StringComparison.OrdinalIgnoreCase) ||
                                 headerText.Contains("response", StringComparison.OrdinalIgnoreCase) ||
                                 headerText.Contains("answer", StringComparison.OrdinalIgnoreCase))
                        {
                            expectedColumn = col;
                        }
                        else if (headerText.Contains("result", StringComparison.OrdinalIgnoreCase) ||
                                 headerText.Contains("status", StringComparison.OrdinalIgnoreCase))
                        {
                            resultColumn = col;
                        }
                    }

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var question = worksheet.Cells[row, questionColumn].Text.Trim();
                        var expected = worksheet.Cells[row, expectedColumn].Text.Trim();
                        var result = worksheet.Cells[row, resultColumn].Text.Trim();

                        if (string.IsNullOrWhiteSpace(question) && string.IsNullOrWhiteSpace(expected))
                        {
                            Console.WriteLine($"ℹ️ Blank row encountered at row {row}. Stopping read.");
                            break;
                        }

                        var dataRow = dataTable.NewRow();
                        dataRow["Question"] = question;
                        dataRow["ExpectedResponse"] = expected;
                        dataRow["Result"] = result;
                        dataTable.Rows.Add(dataRow);
                    }

                    Console.WriteLine($"✅ Loaded {dataTable.Rows.Count} General Queries rows");
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading General Queries Excel file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reads Sheet2 (named "Menu") from GeneralQueries.xlsx.
        /// Expected columns: Menu, Submenu, BotResponse (expected), ..., ActualBotResponse (col E), Result
        /// </summary>
        public DataTable ReadOtherMenuExcel(string filePath)
        {
            try
            {
                Console.WriteLine($"📄 Reading OtherMenu Sheet2 from: {filePath}");
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Excel file not found at {filePath}");

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // Try to get the worksheet named "Menu", fall back to index 1
                    ExcelWorksheet? worksheet = null;
                    foreach (var ws in package.Workbook.Worksheets)
                    {
                        if (string.Equals(ws.Name, "Menu", StringComparison.OrdinalIgnoreCase))
                        {
                            worksheet = ws;
                            break;
                        }
                    }
                    if (worksheet == null && package.Workbook.Worksheets.Count > 1)
                        worksheet = package.Workbook.Worksheets[1];
                    if (worksheet == null)
                        throw new Exception("Could not find Sheet2 / 'Menu' worksheet in the Excel file.");

                    var dataTable = new DataTable();
                    dataTable.Columns.Add("Menu");
                    dataTable.Columns.Add("Submenu");
                    dataTable.Columns.Add("BotResponse");
                    dataTable.Columns.Add("Result");

                    // Detect column indices from header row
                    int menuCol = 1, submenuCol = 2, botResponseCol = 3, resultCol = 0;
                    bool botResponseColSet = false;

                    int lastCol = worksheet.Dimension?.End.Column ?? 0;
                    for (int col = 1; col <= lastCol; col++)
                    {
                        var header = worksheet.Cells[1, col].Text.Trim();
                        if (string.IsNullOrWhiteSpace(header)) continue;

                        if (header.Equals("Menu", StringComparison.OrdinalIgnoreCase))
                            menuCol = col;
                        else if (header.Equals("Submenu", StringComparison.OrdinalIgnoreCase) ||
                                 header.Contains("sub", StringComparison.OrdinalIgnoreCase))
                            submenuCol = col;
                        else if (!header.Contains("actual", StringComparison.OrdinalIgnoreCase) &&
                                 (header.Contains("bot", StringComparison.OrdinalIgnoreCase) ||
                                  header.Contains("response", StringComparison.OrdinalIgnoreCase) ||
                                  header.Contains("expected", StringComparison.OrdinalIgnoreCase)))
                        {
                            // Pick the first response-like column that is NOT "Actual Bot Response"
                            if (!botResponseColSet)
                            {
                                botResponseCol = col;
                                botResponseColSet = true;
                            }
                        }
                        else if (header.Contains("result", StringComparison.OrdinalIgnoreCase) ||
                                 header.Contains("status", StringComparison.OrdinalIgnoreCase))
                            resultCol = col;
                    }

                    int lastRow = worksheet.Dimension?.End.Row ?? 1;
                    for (int row = 2; row <= lastRow; row++)
                    {
                        var menu = worksheet.Cells[row, menuCol].Text.Trim();
                        var submenu = worksheet.Cells[row, submenuCol].Text.Trim();

                        // Stop at fully blank row
                        if (string.IsNullOrWhiteSpace(menu) && string.IsNullOrWhiteSpace(submenu))
                        {
                            Console.WriteLine($"ℹ️ Blank row at {row}. Stopping OtherMenu read.");
                            break;
                        }

                        var botResponse = worksheet.Cells[row, botResponseCol].Text.Trim();
                        var result = resultCol > 0 ? worksheet.Cells[row, resultCol].Text.Trim() : string.Empty;

                        var dataRow = dataTable.NewRow();
                        dataRow["Menu"] = menu;
                        dataRow["Submenu"] = submenu;
                        dataRow["BotResponse"] = botResponse;
                        dataRow["Result"] = result;
                        dataTable.Rows.Add(dataRow);
                    }

                    Console.WriteLine($"✅ Loaded {dataTable.Rows.Count} OtherMenu rows from Sheet2");
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading OtherMenu Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Writes the actual bot response AND the result to the Menu sheet in a single
        /// file-open/save operation, so both values land in the correct adjacent columns.
        /// Creates "Actual Bot Response" and "Result" column headers automatically if absent.
        /// </summary>
        public void WriteOtherMenuRow(string filePath, int rowIndex, string botResponse, string result)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // Locate the Menu worksheet
                    ExcelWorksheet? ws = null;
                    foreach (var sheet in package.Workbook.Worksheets)
                    {
                        if (string.Equals(sheet.Name, "Menu", StringComparison.OrdinalIgnoreCase))
                        { ws = sheet; break; }
                    }
                    if (ws == null && package.Workbook.Worksheets.Count > 1)
                        ws = package.Workbook.Worksheets[1];
                    if (ws == null)
                        throw new Exception("Could not find Sheet2 / 'Menu' worksheet.");

                    // Scan header row for existing column positions
                    int actualCol  = 0;
                    int resultCol  = 0;
                    int lastHdrCol = 0; // last column that has any header text

                    int lastCol = ws.Dimension?.End.Column ?? 0;
                    for (int col = 1; col <= lastCol; col++)
                    {
                        var hdr = ws.Cells[1, col].Text.Trim();
                        if (string.IsNullOrWhiteSpace(hdr)) continue;

                        lastHdrCol = col;

                        if (hdr.Contains("actual", StringComparison.OrdinalIgnoreCase))
                            actualCol = col;
                        else if (hdr.Contains("result", StringComparison.OrdinalIgnoreCase) ||
                                 hdr.Contains("status", StringComparison.OrdinalIgnoreCase))
                            resultCol = col;
                    }

                    // Create "Actual Bot Response" header immediately after the last header column
                    if (actualCol == 0)
                    {
                        actualCol = lastHdrCol + 1;
                        ws.Cells[1, actualCol].Value = "Actual Bot Response";
                        lastHdrCol = actualCol;
                    }

                    // Create "Result" header immediately after "Actual Bot Response"
                    if (resultCol == 0)
                    {
                        resultCol = lastHdrCol + 1;
                        ws.Cells[1, resultCol].Value = "Result";
                    }

                    // Write both values for this row in one save
                    int excelRow = rowIndex + 2; // row 1 = header; data starts at row 2
                    ws.Cells[excelRow, actualCol].Value = botResponse;
                    ws.Cells[excelRow, resultCol].Value = result;

                    package.Save();
                    Console.WriteLine($"✅ OtherMenu row {excelRow}: actual → col {actualCol}, result '{result}' → col {resultCol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing OtherMenu row: {ex.Message}");
                throw;
            }
        }

        // Keep backward-compatible stubs in case they are referenced elsewhere
        public void WriteOtherMenuBotResponse(string filePath, int rowIndex, string botResponse)
            => WriteOtherMenuRow(filePath, rowIndex, botResponse, string.Empty);

        public void WriteOtherMenuResult(string filePath, int rowIndex, string result)
        {
            // Lightweight: open once, find/create Result column, write result only
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet? ws = null;
                    foreach (var sheet in package.Workbook.Worksheets)
                    {
                        if (string.Equals(sheet.Name, "Menu", StringComparison.OrdinalIgnoreCase))
                        { ws = sheet; break; }
                    }
                    if (ws == null && package.Workbook.Worksheets.Count > 1)
                        ws = package.Workbook.Worksheets[1];
                    if (ws == null) return;

                    int resultCol  = 0;
                    int lastHdrCol = 0;
                    int lastCol    = ws.Dimension?.End.Column ?? 0;
                    for (int col = 1; col <= lastCol; col++)
                    {
                        var hdr = ws.Cells[1, col].Text.Trim();
                        if (string.IsNullOrWhiteSpace(hdr)) continue;
                        lastHdrCol = col;
                        if (hdr.Contains("result", StringComparison.OrdinalIgnoreCase) ||
                            hdr.Contains("status", StringComparison.OrdinalIgnoreCase))
                        { resultCol = col; break; }
                    }
                    if (resultCol == 0)
                    {
                        resultCol = lastHdrCol + 1;
                        ws.Cells[1, resultCol].Value = "Result";
                    }

                    int excelRow = rowIndex + 2;
                    ws.Cells[excelRow, resultCol].Value = result;
                    package.Save();
                    Console.WriteLine($"✅ OtherMenu result '{result}' written to row {excelRow}, col {resultCol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing OtherMenu result: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the PermitStatus sheet from PermitStatus.xlsx.
        /// Input columns: Last Name, Post Code, Permit Number
        /// Expected columns: Current Status, Permit Category, Permit Expiry Date, Status Description
        /// </summary>
        public DataTable ReadPermitStatusExcel(string filePath)
        {
            try
            {
                Console.WriteLine($"📄 Reading Permit Status Excel file: {filePath}");
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Excel file not found at {filePath}");

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var dataTable = new DataTable();

                    dataTable.Columns.Add("LastName");
                    dataTable.Columns.Add("PostCode");
                    dataTable.Columns.Add("PermitNumber");
                    dataTable.Columns.Add("CurrentStatus");
                    dataTable.Columns.Add("PermitCategory");
                    dataTable.Columns.Add("PermitExpiryDate");
                    dataTable.Columns.Add("StatusDescription");

                    int lastNameCol = 1, postCodeCol = 2, permitNumCol = 3;
                    int currentStatusCol = 0, categoryCol = 0, expiryCol = 0, descriptionCol = 0;

                    int lastCol = worksheet.Dimension?.End.Column ?? 0;
                    for (int col = 1; col <= lastCol; col++)
                    {
                        var hdr = worksheet.Cells[1, col].Text.Trim();
                        if (string.IsNullOrWhiteSpace(hdr)) continue;
                        var h = hdr.ToLowerInvariant();

                        if (h.Contains("last") || h.Contains("surname"))
                            lastNameCol = col;
                        else if (h.Contains("post") || h.Contains("postcode"))
                            postCodeCol = col;
                        else if (h.Contains("permit") && h.Contains("number"))
                            permitNumCol = col;
                        else if (h.Contains("current") && h.Contains("status"))
                            currentStatusCol = col;
                        else if (h.Contains("category"))
                            categoryCol = col;
                        else if (h.Contains("expiry"))
                            expiryCol = col;
                        else if (h.Contains("description"))
                            descriptionCol = col;
                    }

                    int lastRow = worksheet.Dimension?.End.Row ?? 1;
                    for (int row = 2; row <= lastRow; row++)
                    {
                        var lastName  = worksheet.Cells[row, lastNameCol].Text.Trim();
                        var postCode  = worksheet.Cells[row, postCodeCol].Text.Trim();
                        var permitNum = worksheet.Cells[row, permitNumCol].Text.Trim();

                        if (string.IsNullOrWhiteSpace(lastName) &&
                            string.IsNullOrWhiteSpace(postCode) &&
                            string.IsNullOrWhiteSpace(permitNum))
                        {
                            Console.WriteLine($"ℹ️ Blank row at {row}. Stopping PermitStatus read.");
                            break;
                        }

                        var currentStatus = currentStatusCol > 0 ? worksheet.Cells[row, currentStatusCol].Text.Trim() : string.Empty;
                        var category      = categoryCol      > 0 ? worksheet.Cells[row, categoryCol].Text.Trim()      : string.Empty;
                        var expiry        = expiryCol        > 0 ? worksheet.Cells[row, expiryCol].Text.Trim()        : string.Empty;
                        var description   = descriptionCol   > 0 ? worksheet.Cells[row, descriptionCol].Text.Trim()   : string.Empty;

                        var dataRow = dataTable.NewRow();
                        dataRow["LastName"]          = lastName;
                        dataRow["PostCode"]          = postCode;
                        dataRow["PermitNumber"]      = permitNum;
                        dataRow["CurrentStatus"]     = currentStatus;
                        dataRow["PermitCategory"]    = category;
                        dataRow["PermitExpiryDate"]  = expiry;
                        dataRow["StatusDescription"] = description;
                        dataTable.Rows.Add(dataRow);
                    }

                    Console.WriteLine($"✅ Loaded {dataTable.Rows.Count} PermitStatus rows");
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading Permit Status Excel file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Writes the full bot response text and overall match result to the PermitStatus Excel file.
        /// Auto-creates "Actual Response" and "Result" columns if absent.
        /// </summary>
        public void WritePermitStatusRow(string filePath, int rowIndex, string botResponse, string result)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets[0];

                    int actualCol = 0, resultCol = 0, lastHdrCol = 0;
                    int lastCol = ws.Dimension?.End.Column ?? 0;

                    for (int col = 1; col <= lastCol; col++)
                    {
                        var hdr = ws.Cells[1, col].Text.Trim();
                        if (string.IsNullOrWhiteSpace(hdr)) continue;
                        lastHdrCol = col;

                        if (hdr.Contains("actual", StringComparison.OrdinalIgnoreCase))
                            actualCol = col;
                        else if (hdr.Equals("Result", StringComparison.OrdinalIgnoreCase))
                            resultCol = col;
                    }

                    if (actualCol == 0)
                    {
                        actualCol = lastHdrCol + 1;
                        ws.Cells[1, actualCol].Value = "Actual Response";
                        lastHdrCol = actualCol;
                    }

                    if (resultCol == 0)
                    {
                        resultCol = lastHdrCol + 1;
                        ws.Cells[1, resultCol].Value = "Result";
                    }

                    int excelRow = rowIndex + 2;
                    ws.Cells[excelRow, actualCol].Value = botResponse;
                    ws.Cells[excelRow, resultCol].Value = result;

                    package.Save();
                    Console.WriteLine($"✅ PermitStatus row {excelRow} written: result='{result}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing PermitStatus row: {ex.Message}");
                throw;
            }
        }
    }
}