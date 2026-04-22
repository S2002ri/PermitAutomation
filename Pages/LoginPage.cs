using Microsoft.Playwright;
using System.Threading.Tasks;
using PlaywrightAutomation.Utils;

namespace PlaywrightAutomation.Pages
{
    public class LoginPage
    {
        private readonly IPage _page;
        private readonly LocatorReader _locatorReader;

        public LoginPage(IPage page)
        {
            _page = page;
            _locatorReader = new LocatorReader();
        }

        public async Task OpenUrlAsync(string url)
        {
            await _page.GotoAsync(url);
        }

        public async Task<bool> IsChatBotLoadedAsync()
        {
            try
            {
                Console.WriteLine("Waiting for page load...");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                Console.WriteLine("Looking for chatbot button...");
                var allButtons = _page.Locator("button");
                var buttonCount = await allButtons.CountAsync();
                Console.WriteLine($"Total buttons found: {buttonCount}");

                var processedButtons = new HashSet<string>();

                for (int i = 0; i < buttonCount; i++)
                {
                    var buttonText = await allButtons.Nth(i).InnerTextAsync();
                    if (!processedButtons.Contains(buttonText))
                    {
                        Console.WriteLine($"Processing Button {i}: {buttonText}");
                        processedButtons.Add(buttonText);
                    }
                }

                var chatbotButton = _page.Locator("button:has(img.chatbot-btn-avatar), button:has-text('comments_disabled')").First;

                try
                {
                    // Wait for the chatbot icon to become visible
                    await _page.WaitForSelectorAsync("img.chatbot-btn-avatar", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
                    Console.WriteLine("Found chatbot icon, clicking...");
                    await _page.ClickAsync("img.chatbot-btn-avatar");
                    await Task.Delay(3000); // Wait for chat interface to open
                    return true;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Chatbot icon did not become visible within the timeout period.");
                }

                // Continue execution with CheckParkingStreet
                var checkParkingStreet = new CheckParkingStreet(_page);
                if (await checkParkingStreet.IsCardButtonVisibleAsync())
                {
                    Console.WriteLine("Check Parking Street card button is visible, clicking...");
                    await checkParkingStreet.OpenCardAsync();
                }
                else
                {
                    Console.WriteLine("Check Parking Street card button is not visible.");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsChatBotLoadedAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsGreetingMessageVisibleAsync()
        {
            try
            {
                var greetingLocator = _page.Locator("#councilBasedWelcomeMessage");

                await greetingLocator.WaitForAsync(new()
                {
                    Timeout = 15000
                });

                return await greetingLocator.IsVisibleAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking greeting message: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsCardButtonVisibleAsync()
        {
            var cardBtnLocator = _locatorReader.GetLocator("CheckParkingStreetCardButton");
            Console.WriteLine($"Checking visibility of locator: {cardBtnLocator}");

            // Check if the element exists
            var elementExists = await _page.Locator(cardBtnLocator).CountAsync() > 0;
            Console.WriteLine($"Element exists: {elementExists}");

            if (!elementExists)
            {
                Console.WriteLine("Element does not exist on the page.");
                return false;
            }

            // Check visibility
            var isVisible = await _page.IsVisibleAsync(cardBtnLocator);
            Console.WriteLine($"IsCardButtonVisibleAsync: {isVisible}");
            return isVisible;
        }

        public async Task ClickCardButtonAsync()
        {
            var cardBtnLocator = _locatorReader.GetLocator("CheckParkingStreetCardButton");
            Console.WriteLine($"Clicking on locator: {cardBtnLocator}");
            await _page.ClickAsync(cardBtnLocator);
        }

        public async Task<bool> IsCardContentVisibleAsync()
        {
            var cardContentLocator = _locatorReader.GetLocator("CheckParkingStreetCardContent");
            Console.WriteLine($"Checking visibility of locator: {cardContentLocator}");

            // Check if the element exists
            var elementExists = await _page.Locator(cardContentLocator).CountAsync() > 0;
            Console.WriteLine($"Element exists: {elementExists}");

            if (!elementExists)
            {
                Console.WriteLine("Element does not exist on the page.");
                return false;
            }

            // Check visibility
            var isVisible = await _page.IsVisibleAsync(cardContentLocator);
            Console.WriteLine($"IsCardContentVisibleAsync: {isVisible}");
            return isVisible;
        }

        public async Task<bool> IsAdaptiveCardFormVisibleAsync()
        {
            try
            {
                // Wait for the first input field to appear (indicates form is loading)
                await _page.Locator("input[type='text']").First.WaitForAsync(new() { Timeout = 10000, State = WaitForSelectorState.Visible });
                
                // Short delay for all fields to render
                await Task.Delay(500);
                
                // Check if all three input fields are visible
                var firstNameField = await _page.Locator("input[type='text']").First.IsVisibleAsync();
                var lastNameField = await _page.Locator("input[type='text']").Nth(1).IsVisibleAsync();
                var mobileField = await _page.Locator("input[type='text']").Nth(2).IsVisibleAsync();
                
                return firstNameField && lastNameField && mobileField;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Form fields not visible: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> FillFormAndSubmit(string firstName, string lastName, string mobileNo)
        {
            try
            {
                // Fill the form fields if they are not empty
                if (!string.IsNullOrEmpty(firstName))
                    await _page.Locator("input[type='text']").First.FillAsync(firstName);
                if (!string.IsNullOrEmpty(lastName))
                    await _page.Locator("input[type='text']").Nth(1).FillAsync(lastName);
                if (!string.IsNullOrEmpty(mobileNo))
                    await _page.Locator("input[type='text']").Nth(2).FillAsync(mobileNo);
                
                // If all fields were provided, try to click submit
                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName) && !string.IsNullOrEmpty(mobileNo))
                {
                    var submitButton = await _page.Locator("button:has-text('Submit')").IsEnabledAsync();
                    if (submitButton)
                    {
                        await _page.Locator("button:has-text('Submit')").ClickAsync();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateFirstNameFieldAsync(string input)
        {
            var firstNameLocator = _locatorReader.GetLocator("FirstName");
            await _page.FillAsync(firstNameLocator, input);
            var value = await _page.InputValueAsync(firstNameLocator);
            return value.All(char.IsLetter) && value.Length <= 25;
        }

        public async Task<bool> ValidateLastNameFieldAsync(string input)
        {
            var lastNameLocator = _locatorReader.GetLocator("LastName");
            await _page.FillAsync(lastNameLocator, input);
            var value = await _page.InputValueAsync(lastNameLocator);
            return value.All(char.IsLetter) && value.Length <= 25;
        }

        public async Task<bool> ValidateMobileNumberFieldAsync(string input)
        {
            var mobileNoLocator = _locatorReader.GetLocator("MobileNo");
            await _page.FillAsync(mobileNoLocator, input);
            var value = await _page.InputValueAsync(mobileNoLocator);
            return value.All(char.IsDigit) && value.Length <= 11;
        }

        public async Task<bool> IsSubmitButtonEnabled()
        {
            return !await _page.IsDisabledAsync(_locatorReader.GetLocator("SubmitButton"));
        }

        public async Task FillForm(string firstName, string lastName, string mobileNo)
        {
            await _page.FillAsync(_locatorReader.GetLocator("FirstName"), firstName);
            await _page.FillAsync(_locatorReader.GetLocator("LastName"), lastName);
            await _page.FillAsync(_locatorReader.GetLocator("MobileNo"), mobileNo);
        }

        public async Task ClickSubmit()
        {
            await _page.ClickAsync(_locatorReader.GetLocator("SubmitButton"));
        }

        public async Task<bool> IsMainCardOptionsVisible()
        {
            try
            {
                // Wait for ANY main menu card to appear
                await _page.WaitForSelectorAsync("button[id^='menuCard']", new() { Timeout = 20000 });

                // Define expected options
                string[] expectedOptions =
                {
                    "Check Parking Streets",
                    "Check Permit Status",
                    "Find Correct Zone",
                    "General Queries",
                    "Account Management",
                    "Payment & Fee"
                };

                // Validate visibility of all expected options
                foreach (var option in expectedOptions)
                {
                    var locator = _page.Locator($"text={option}").First;
                    if (!await locator.IsVisibleAsync())
                    {
                        Console.WriteLine($"Option '{option}' is not visible.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Main menu cards not visible: {ex.Message}");
                return false;
            }
        }
    }
}
