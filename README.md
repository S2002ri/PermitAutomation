# PlaywrightAutomation

This is a C# Playwright automation framework for chatbot testing.

## Structure
- **Base/**: Browser initialization
- **Pages/**: Page objects (LoginPage)
- **Utils/**: Excel and locator utilities
- **TestData/**: Excel files for test data and test cases
- **Tests/**: Test implementations (LoginTest)
- **Reporting/**: ExtentReport integration
- **Program.cs**: Main entry point
- **playwright.config.json**: Playwright configuration
- **PlaywrightAutomation.csproj**: Project file

## How to Run
1. Restore NuGet packages
2. Ensure test data and test case Excel files are populated
3. Build and run the project

## Test Data Format
**TestData.xlsx**
| FirstName | LastName | MobileNo | LoginUrl | ChatInputBox |
|-----------|----------|----------|----------|--------------|
| Saranya   | Eswaran  | 8072424475 | https://mia.permit.marstonholdings.co.uk/C1CA422B-A4CC-4B26-B9FD-4280CD69E743 | w1 |

**TestCases.xlsx**
| TestcaseID | TestScenario | Status |
|------------|-------------|--------|
| 96937 | Check whether the Permit bot URL gets loaded | |
| 110841 | Verify that the chatbot displays a greeting message upon opening. | |
| 110842 | Verify chatbot prompt and adaptive card form with fields | |
| 110843 | Verify first name field accepts only alphabets, sanitizes numeric, max 25 chars | |
| 110844 | Verify last name field accepts only alphabets, sanitizes numeric, max 25 chars | |
| 110845 | Verify mobile number field accepts only numbers, sanitizes non-numeric, max 11 digits | |
| 110846 | Verify submit button enabled only when all fields are filled, disabled otherwise | |
| 110847 | Verify submitting valid form leads to main card options | |

## Locators
| ElementName         | LocatorValue              |
|--------------------|--------------------------|
| ChatBotIcon        | img.chatbot-btn-avatar    |
| FirstName          | #user-first-name          |
| LastName           | #user-lastname            |
| MobileNo           | #user-mobilenumber        |
| SubmitButton       | #btn-user-submit          |
| ChatbotMenuOption  | #menuCard0                |
| ChatInputBox       | #txt_Chat                 |
| SendButton         | #btnImage                 |

## Reporting
Test results are updated in TestCases.xlsx and an HTML report is generated in Reporting/.
