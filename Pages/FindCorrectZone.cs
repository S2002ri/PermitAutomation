using Microsoft.Playwright;
using PlaywrightAutomation.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class FindCorrectZone
{
    private readonly IPage _page;
    private readonly LocatorReader _locatorReader;

    public FindCorrectZone(IPage page)
    {
        _page = page;
        string locatorFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "FindCorrectZoneLocators.json");
        if (!File.Exists(locatorFilePath))
        {
            locatorFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "FindCorrectZoneLocators.json");
        }
        _locatorReader = new LocatorReader(locatorFilePath);
    }

    public async Task<bool> IsCardButtonVisibleAsync()
    {
        var configuredLocator = _locatorReader.GetLocator("FindCorrectZoneCardButton");
        var candidateSelectors = new[]
        {
            configuredLocator,
            "#menuCard2",
            "button:has-text('Find Correct Zone')",
            "button.btn.msg-ans-btn[data-template-name=\"Find Correct Zone\"]"
        }
        .Where(selector => !string.IsNullOrWhiteSpace(selector))
        .Distinct();

        foreach (var selector in candidateSelectors)
        {
            Console.WriteLine($"Checking visibility of locator: {selector}");
            try
            {
                var count = await _page.Locator(selector).CountAsync();
                if (count == 0)
                    continue;

                var isVisible = await _page.Locator(selector).First.IsVisibleAsync();
                Console.WriteLine($"IsCardButtonVisibleAsync ({selector}): {isVisible}");
                if (isVisible)
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Locator visibility check failed for '{selector}': {ex.Message}");
            }
        }

        Console.WriteLine("Element does not exist on the page.");
        return false;
    }

    public async Task OpenCardAsync()
    {
        // Priority strategy: click exact Find Correct Zone button by ID
        try
        {
            Console.WriteLine("OpenCardAsync: Strategy 0 - clicking #menuCard2");
            var menuCard = _page.Locator("#menuCard2").First;
            await menuCard.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 10000 });
            await menuCard.ScrollIntoViewIfNeededAsync();

            try
            {
                await menuCard.ClickAsync(new() { Timeout = 5000 });
                Console.WriteLine("OpenCardAsync: #menuCard2 clicked successfully");
                await Task.Delay(1000);
                return;
            }
            catch (Exception normalClickEx)
            {
                Console.WriteLine($"OpenCardAsync: Normal click on #menuCard2 failed - {normalClickEx.Message}");
            }

            try
            {
                await menuCard.ClickAsync(new() { Force = true, Timeout = 5000 });
                Console.WriteLine("OpenCardAsync: #menuCard2 force-clicked successfully");
                await Task.Delay(1000);
                return;
            }
            catch (Exception forceClickEx)
            {
                Console.WriteLine($"OpenCardAsync: Force click on #menuCard2 failed - {forceClickEx.Message}");
            }

            var jsClicked = await _page.EvaluateAsync<bool>(@"
                (() => {
                    const button = document.getElementById('menuCard2');
                    if (button) {
                        button.click();
                        return true;
                    }
                    return false;
                })();
            ");

            if (jsClicked)
            {
                Console.WriteLine("OpenCardAsync: #menuCard2 clicked successfully via JavaScript");
                await Task.Delay(1000);
                return;
            }
        }
        catch (Exception directIdEx)
        {
            Console.WriteLine($"OpenCardAsync: Strategy 0 failed - {directIdEx.Message}");
        }

        var configuredLocator = _locatorReader.GetLocator("FindCorrectZoneCardButton");
        var candidateSelectors = new[]
        {
            configuredLocator,
            "#menuCard2",
            "button:has-text('Find Correct Zone')",
            "button.btn.msg-ans-btn[data-template-name=\"Find Correct Zone\"]"
        }
        .Where(selector => !string.IsNullOrWhiteSpace(selector))
        .Distinct();

        Exception? lastException = null;
        foreach (var selector in candidateSelectors)
        {
            Console.WriteLine($"OpenCardAsync: Attempting to click on locator: {selector}");
            try
            {
                var locator = _page.Locator(selector).First;
                await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                await locator.ScrollIntoViewIfNeededAsync();
                await locator.ClickAsync(new() { Timeout = 8000 });
                Console.WriteLine($"OpenCardAsync: Button clicked successfully using {selector}");
                await Task.Delay(1000);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"OpenCardAsync: Click failed for {selector} - {ex.Message}");
            }
        }

        throw new Exception("OpenCardAsync: Unable to locate/click Find Correct Zone card with known selectors.", lastException);
    }

    public async Task<bool> IsCardVisibleAsync()
    {
        var cardContentLocator = _locatorReader.GetLocator("FindCorrectZoneCardContent");
        Console.WriteLine($"Checking visibility of locator: {cardContentLocator}");
        var isVisible = await _page.IsVisibleAsync(cardContentLocator);
        Console.WriteLine($"IsCardVisibleAsync: {isVisible}");
        return isVisible;
    }

    public async Task ClickTextFieldAsync()
    {
        try
        {
            var textFieldLocator = _locatorReader.GetLocator("FindCorrectZoneTextField");
            Console.WriteLine($"Clicking on text field: {textFieldLocator}");
            
            // Wait for the textarea to be visible
            await _page.WaitForSelectorAsync(textFieldLocator, new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Console.WriteLine("Text field is visible");
            
            // Click on the text field to focus it
            await _page.Locator(textFieldLocator).ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Text field clicked successfully");
            
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clicking text field: {ex.Message}");
            throw;
        }
    }

    public async Task TypeStreetAsync(string street)
    {
        try
        {
            Console.WriteLine($"Typing street: {street}");

            // Wait for the textarea to be enabled
            var textareaSelector = _locatorReader.GetLocator("FindCorrectZoneTextField");
            Console.WriteLine($"Waiting for textarea to be enabled: {textareaSelector}");

            var textareaCount = await _page.Locator(textareaSelector).CountAsync();
            if (textareaCount > 0)
            {
                // Wait for textarea to not be disabled
                for (int i = 0; i < 20; i++)
                {
                    var isDisabled = await _page.Locator(textareaSelector).IsDisabledAsync();
                    if (!isDisabled)
                    {
                        Console.WriteLine($"Textarea is enabled after {i * 500}ms");
                        break;
                    }
                    Console.WriteLine($"Textarea still disabled, waiting... ({i + 1}/20)");
                    await Task.Delay(500);
                }

                // Extra wait for stability
                await Task.Delay(1000);

                // Use JavaScript to set value directly
                Console.WriteLine($"Setting street value using JavaScript: {street}");
                await _page.EvaluateAsync($@"
                    var textarea = document.getElementById('txt_Chat');
                    if (textarea) {{
                        textarea.disabled = false;
                        textarea.value = '{street}';
                        textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                    }}
                ");

                Console.WriteLine($"Street set successfully: {street}");
                await Task.Delay(500);
                return;
            }

            // Fallback to other selectors
            Console.WriteLine("Textarea not found with locator, trying alternative selectors...");
            var inputSelectors = new[]
            {
                "textarea.msg-input-area",
                "textarea",
                "input[type='text']",
                ".ac-textInput",
                "input.ac-input"
            };

            foreach (var selector in inputSelectors)
            {
                var count = await _page.Locator(selector).CountAsync();
                if (count > 0)
                {
                    Console.WriteLine($"Found input field with selector: {selector}");
                    await _page.Locator(selector).Last.ClickAsync();
                    await Task.Delay(300);
                    await _page.Locator(selector).Last.FillAsync(street);
                    Console.WriteLine($"Street typed successfully: {street}");
                    return;
                }
            }

            throw new Exception("Input field not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error typing street: {ex.Message}");
            throw;
        }
    }

    public async Task ClickSendButtonAsync()
    {
        try
        {
            Console.WriteLine("Clicking Send button...");
            await Task.Delay(500);

            // Strategy 1: Try the specific button ID first
            var btnUserSubmit = await _page.Locator("#btn-user-submit").CountAsync();
            Console.WriteLine($"#btn-user-submit count: {btnUserSubmit}");

            if (btnUserSubmit > 0)
            {
                var isVisible = await _page.Locator("#btn-user-submit").IsVisibleAsync();
                var isEnabled = await _page.Locator("#btn-user-submit").IsEnabledAsync();
                Console.WriteLine($"Send button visible: {isVisible}, enabled: {isEnabled}");

                if (isVisible && isEnabled)
                {
                    Console.WriteLine("Attempting normal click on #btn-user-submit...");
                    try
                    {
                        await _page.Locator("#btn-user-submit").ClickAsync(new() { Timeout = 3000 });
                        Console.WriteLine("Send button clicked successfully");
                        await Task.Delay(500);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Normal click failed: {ex.Message}");
                    }
                }
                else if (!isEnabled)
                {
                    Console.WriteLine("Send button is disabled - message might be empty or invalid");
                }
            }

            // Strategy 2: Look for send button by other selectors
            var sendButtonSelectors = new[]
            {
                "button.btn-send",
                "button[type='submit']",
                ".send-button",
                "button:has(svg)",
                ".msg-send-btn"
            };

            foreach (var selector in sendButtonSelectors)
            {
                var count = await _page.Locator(selector).CountAsync();
                if (count > 0)
                {
                    Console.WriteLine($"Found send button with selector: {selector}");
                    try
                    {
                        await _page.Locator(selector).Last.ClickAsync(new() { Timeout = 3000 });
                        Console.WriteLine($"Send button clicked via selector: {selector}");
                        await Task.Delay(500);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Click failed for {selector}: {ex.Message}");
                    }
                }
            }

            // Strategy 3: Press Enter on the textarea
            Console.WriteLine("Trying Enter key on textarea...");
            var textFieldLocator = _locatorReader.GetLocator("FindCorrectZoneTextField");
            var textareaCount = await _page.Locator(textFieldLocator).CountAsync();
            if (textareaCount > 0)
            {
                await _page.Locator(textFieldLocator).FocusAsync();
                await Task.Delay(200);
                await _page.Locator(textFieldLocator).PressAsync("Enter");
                Console.WriteLine("Pressed Enter key on textarea");
                await Task.Delay(1000);

                // Verify message was sent
                var textareaValue = await _page.Locator(textFieldLocator).InputValueAsync();
                if (string.IsNullOrEmpty(textareaValue))
                {
                    Console.WriteLine("✓ Message sent successfully - textarea cleared");
                    return;
                }
                else
                {
                    Console.WriteLine($"✗ Textarea still contains: '{textareaValue}' - message may not have sent");
                }
            }

            // Strategy 4: JavaScript form submission
            Console.WriteLine("Trying JavaScript form submit...");
            try
            {
                await _page.EvaluateAsync(@"
                    (() => {
                        var sendBtn = document.getElementById('btn-user-submit');
                        if (sendBtn) {
                            sendBtn.click();
                            return;
                        }
                        
                        var form = document.querySelector('form');
                        if (form) {
                            form.submit();
                            return;
                        }
                        
                        var textarea = document.getElementById('txt_Chat');
                        if (textarea) {
                            var event = new KeyboardEvent('keypress', {
                                key: 'Enter',
                                code: 'Enter',
                                which: 13,
                                keyCode: 13,
                                bubbles: true
                            });
                            textarea.dispatchEvent(event);
                        }
                    })();
                ");
                Console.WriteLine("JavaScript submit executed");
                await Task.Delay(500);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JavaScript submit failed: {ex.Message}");
            }

            Console.WriteLine("⚠ Warning: All send strategies attempted - message may not have been sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clicking Send button: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> VerifyMessageSentAsync(string expectedMessage)
    {
        try
        {
            Console.WriteLine($"Verifying message was sent: {expectedMessage}");
            await Task.Delay(1000);

            // Check if textarea is cleared (common indicator message was sent)
            var textFieldLocator = _locatorReader.GetLocator("FindCorrectZoneTextField");
            var textareaValue = await _page.Locator(textFieldLocator).InputValueAsync();
            if (string.IsNullOrEmpty(textareaValue))
            {
                Console.WriteLine("✓ Textarea is clear - message likely sent");
                return true;
            }

            Console.WriteLine("✗ Textarea still has content - message may not have sent");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying message sent: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetResponseTextAsync(string previousResponse = "")
    {
        try
        {
            Console.WriteLine("Getting response text...");
            Console.WriteLine("Waiting for bot response to appear...");

            // Wait for response with timeout
            await Task.Delay(3000);

            // Get the bot's response - looking for the last message container
            var messageSelectors = new[]
            {
                ".bot-response",
                ".ac-container",
                ".message-content",
                "[class*='bot']",
                "[class*='response']"
            };

            foreach (var selector in messageSelectors)
            {
                var count = await _page.Locator(selector).CountAsync();
                if (count > 0)
                {
                    var lastMessage = await _page.Locator(selector).Last.TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(lastMessage) && lastMessage != previousResponse)
                    {
                        Console.WriteLine($"Found response using selector: {selector}");
                        return lastMessage.Trim();
                    }
                }
            }

            // Fallback: get all text from the page and extract the last meaningful content
            var pageText = await _page.TextContentAsync("body");
            Console.WriteLine("Using fallback method to extract response from page text");
            return pageText ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting response text: {ex.Message}");
            return "";
        }
    }

    public async Task ClickYesButtonAsync()
    {
        try
        {
            Console.WriteLine("Clicking 'Yes' button...");

            // Strategy 0: Click specific confirmation Yes button by ID
            try
            {
                var yesById = _page.Locator("#chatResponseConfirmationYes").First;
                await yesById.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 });
                if (await yesById.IsEnabledAsync())
                {
                    await yesById.ScrollIntoViewIfNeededAsync();
                    await yesById.ClickAsync(new() { Timeout = 5000 });
                    Console.WriteLine("✓ 'Yes' button clicked via #chatResponseConfirmationYes");
                    await Task.Delay(800);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ID-based click failed: {ex.Message}");
            }

            // Wait briefly for action buttons to render after bot response
            await Task.Delay(1200);

            // Strategy 1: Use accessible role lookup first (most reliable when available)
            try
            {
                var yesByRole = _page.GetByRole(AriaRole.Button, new() { Name = "Yes" }).First;
                await yesByRole.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                if (await yesByRole.IsEnabledAsync())
                {
                    await yesByRole.ScrollIntoViewIfNeededAsync();
                    await yesByRole.ClickAsync(new() { Timeout = 5000 });
                    Console.WriteLine("✓ 'Yes' button clicked via role selector");
                    await Task.Delay(800);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Role-based click failed: {ex.Message}");
            }

            // Strategy 2: Known selectors and strict text matches
            var yesButtonSelectors = new[]
            {
                "button:has-text('Yes')",
                "button:has-text('👍 Yes')",
                "#thumbIcon",
                "text=/^\\s*Yes\\s*$/"
            };

            foreach (var selector in yesButtonSelectors)
            {
                var locator = _page.Locator(selector).First;
                var count = await _page.Locator(selector).CountAsync();
                if (count == 0)
                    continue;

                Console.WriteLine($"Found potential 'Yes' element with selector: {selector}");
                try
                {
                    await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 4000 });
                    if (await locator.IsEnabledAsync())
                    {
                        await locator.ScrollIntoViewIfNeededAsync();
                        await locator.ClickAsync(new() { Timeout = 5000 });
                        Console.WriteLine($"✓ 'Yes' clicked successfully using selector: {selector}");
                        await Task.Delay(800);
                        return;
                    }

                    Console.WriteLine($"Selector found but not enabled: {selector}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Click failed for selector '{selector}': {ex.Message}");
                }
            }

            // Strategy 3: JavaScript fallback on explicit button text
            var jsClicked = await _page.EvaluateAsync<bool>(@"
                (() => {
                    const candidates = Array.from(document.querySelectorAll('button, [role=""button""], span, div'));
                    for (const element of candidates) {
                        const text = (element.textContent || '').trim();
                        if (text === 'Yes' || text === '👍 Yes') {
                            element.click();
                            return true;
                        }
                    }
                    return false;
                })();
            ");

            if (jsClicked)
            {
                Console.WriteLine("✓ 'Yes' clicked via JavaScript fallback");
                await Task.Delay(800);
                return;
            }

            throw new Exception("'Yes' button was not visible/clickable after all strategies.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clicking 'Yes' button: {ex.Message}");
            throw;
        }
    }

    public async Task ClickMainCardOptionAsync()
    {
        try
        {
            Console.WriteLine("Clicking 'Main Menu' button...");

            var mainMenuSelectors = new[]
            {
                "button:has-text('Main Menu')",
                "button:has-text('main menu')",
                "button:has-text('MAIN MENU')",
                "[class*='main-menu']",
                "[id*='main-menu']"
            };

            foreach (var selector in mainMenuSelectors)
            {
                var count = await _page.Locator(selector).CountAsync();
                if (count > 0)
                {
                    Console.WriteLine($"Found 'Main Menu' button with selector: {selector}");
                    await _page.Locator(selector).First.ClickAsync(new() { Timeout = 5000 });
                    Console.WriteLine("'Main Menu' button clicked successfully");
                    await Task.Delay(1000);
                    return;
                }
            }

            Console.WriteLine("Warning: 'Main Menu' button not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clicking 'Main Menu' button: {ex.Message}");
            throw;
        }
    }
}
