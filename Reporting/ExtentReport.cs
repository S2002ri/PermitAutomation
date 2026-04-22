using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;

namespace PlaywrightAutomation.Reporting
{
    public class ExtentReport
    {
        private const string DefaultTestName = "General";
        private ExtentReports? _extent;
        private ExtentTest? _parentTest;
        private ExtentTest? _test;

        public void InitReport(string reportPath)
        {
            var sparkReporter = new AventStack.ExtentReports.Reporter.ExtentSparkReporter(reportPath);
            _extent = new ExtentReports();
            _extent.AttachReporter(sparkReporter);
        }

        /// <summary>
        /// Creates a top-level category node in the consolidated report.
        /// All subsequent CreateTest() calls will appear as child nodes under it.
        /// </summary>
        public void SetCategory(string categoryName)
        {
            if (_extent == null)
                throw new InvalidOperationException("Report is not initialized. Call InitReport() before SetCategory().");
            _parentTest = _extent.CreateTest(string.IsNullOrWhiteSpace(categoryName) ? DefaultTestName : categoryName);
            _test = null;
        }

        public void CreateTest(string testName)
        {
            if (_extent == null)
                throw new InvalidOperationException("Report is not initialized. Call InitReport() before CreateTest().");

            var name = string.IsNullOrWhiteSpace(testName) ? DefaultTestName : testName;
            _test = _parentTest != null
                ? _parentTest.CreateNode(name)
                : _extent.CreateTest(name);
        }

        public void Log(string status, string details)
        {
            if (_extent == null)
                throw new InvalidOperationException("Report is not initialized. Call InitReport() before Log().");

            _test ??= _extent.CreateTest(DefaultTestName);

            var normalizedStatus = status?.Trim();
            if (string.Equals(normalizedStatus, "Pass", StringComparison.OrdinalIgnoreCase))
                _test.Pass(details);
            else if (string.Equals(normalizedStatus, "Fail", StringComparison.OrdinalIgnoreCase))
                _test.Fail(details);
            else
                _test.Info(details);
        }

        public void Flush()
        {
            _extent?.Flush();
        }
    }
}
