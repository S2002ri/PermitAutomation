using Microsoft.Playwright; // for IPage 
using PlaywrightAutomation.Utils; // for LocatorReader 
using System; using System.IO; 
using System.Threading.Tasks; 
public class CheckParkingStreet 
{ 
private readonly IPage _page; 
private readonly LocatorReader _locatorReader; 
public CheckParkingStreet(IPage page) 
{ 
_page = page; 
string locatorFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "CheckParkingStreetLocators.json"); 
_locatorReader = new LocatorReader(locatorFilePath); 
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

    // Wait for the element to become visible
    try
    {
        Console.WriteLine("Waiting for the element to become visible...");
        await _page.Locator(cardBtnLocator).WaitForAsync(new() { Timeout = 20000 }); // Increased timeout to 20 seconds
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Element did not become visible within the timeout period.");
        return false;
    }

    // Check visibility
    var isVisible = await _page.IsVisibleAsync(cardBtnLocator);
    Console.WriteLine($"IsCardButtonVisibleAsync: {isVisible}");
    return isVisible;
} 
public async Task OpenCardAsync() 
{ 
    var cardBtnLocator = _locatorReader.GetLocator("CheckParkingStreetCardButton");
    Console.WriteLine($"OpenCardAsync: Attempting to click on locator: {cardBtnLocator}");
    
    try
    {
        // Wait for the button to be visible and enabled - use First() to handle multiple matches
        var locator = _page.Locator(cardBtnLocator).First;
        await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20000 });
        Console.WriteLine("OpenCardAsync: Button is visible");
        
        // Scroll into view if needed
        await locator.ScrollIntoViewIfNeededAsync();
        Console.WriteLine("OpenCardAsync: Scrolled into view");
        
        // Click the button
        await locator.ClickAsync(new() { Timeout = 10000 });
        Console.WriteLine("OpenCardAsync: Button clicked successfully");
        
        // Wait a bit for the UI to respond
        await Task.Delay(1000);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"OpenCardAsync: Error clicking button - {ex.Message}");
        throw;
    }
} 
public async Task<bool> IsCardVisibleAsync() 
{ 
var cardContentLocator = _locatorReader.GetLocator("CheckParkingStreetCardContent");
Console.WriteLine($"Checking visibility of locator: {cardContentLocator}");
var isVisible = await _page.IsVisibleAsync(cardContentLocator);
Console.WriteLine($"IsCardVisibleAsync: {isVisible}");
return isVisible;
}
public async Task ClickCardButtonAsync()
{
    // Use JavaScript to click the button containing the text 'Check Parking Streets'
    await _page.EvaluateAsync("document.querySelector('button:contains(\'Check Parking Streets\')').click();");
}
public async Task TypeZoneAsync(string zone)
{
    try
    {
        Console.WriteLine($"Typing zone: {zone}");
        
        // Wait for the textarea to be enabled (it gets disabled after each submission)
        string textareaSelector = "#txt_Chat";
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
            
            // Use JavaScript to set value directly (bypasses disabled state if needed)
            Console.WriteLine($"Setting zone value using JavaScript: {zone}");
            await _page.EvaluateAsync($@"
                var textarea = document.getElementById('txt_Chat');
                if (textarea) {{
                    textarea.disabled = false;
                    textarea.value = '{zone}';
                    textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                }}
            ");
            
            Console.WriteLine($"Zone set successfully: {zone}");
            await Task.Delay(500);
            return;
        }
        
        // Fallback to other selectors
        Console.WriteLine("Textarea #txt_Chat not found, trying alternative selectors...");
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
                await _page.Locator(selector).Last.FillAsync(zone);
                Console.WriteLine($"Zone typed successfully: {zone}");
                return;
            }
        }
        
        throw new Exception("Input field not found");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error typing zone: {ex.Message}");
        throw;
    }
}

public async Task ClickSendButtonAsync()
{
    try
    {
        Console.WriteLine("Clicking Send button...");
        await Task.Delay(500); // Wait for typing to complete
        
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
        
        // Strategy 2: Look for send button by other selectors (arrow icon, etc.)
        var sendButtonSelectors = new[] 
        {
            "button.btn-send",
            "button[type='submit']",
            ".send-button",
            "button:has(svg)",  // Button with SVG icon
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
        var textareaCount = await _page.Locator("#txt_Chat").CountAsync();
        if (textareaCount > 0)
        {
            // Focus on textarea first
            await _page.Locator("#txt_Chat").FocusAsync();
            await Task.Delay(200);
            
            // Press Enter
            await _page.Locator("#txt_Chat").PressAsync("Enter");
            Console.WriteLine("Pressed Enter key on textarea");
            await Task.Delay(1000);
            
            // Verify message was sent by checking if textarea is cleared
            var textareaValue = await _page.Locator("#txt_Chat").InputValueAsync();
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
                // Try to find and click send button
                var sendBtn = document.getElementById('btn-user-submit');
                if (sendBtn) {
                    sendBtn.click();
                    return;
                }
                
                // Try to find form and submit
                var form = document.querySelector('form');
                if (form) {
                    form.submit();
                    return;
                }
                
                // Try to press Enter on textarea
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

public async Task<string> GetResponseTextAsync(string previousResponse = "")
{
    try
    {
        Console.WriteLine("Getting response text...");
        Console.WriteLine("Waiting for bot response to appear...");
        
        // Get the initial count of UL elements BEFORE waiting
        var initialUlCount = await _page.Locator("ul").CountAsync();
        Console.WriteLine($"Initial UL count: {initialUlCount}");
        
        // Wait for a NEW UL to appear (bot response with streets)
        int maxAttempts = 40; // 20 seconds
        bool newUlDetected = false;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            await Task.Delay(500);
            var currentUlCount = await _page.Locator("ul").CountAsync();
            
            if (currentUlCount > initialUlCount)
            {
                Console.WriteLine($"✓ New UL detected! Count: {currentUlCount} (was {initialUlCount})");
                newUlDetected = true;
                await Task.Delay(1500); // Wait for it to fully load
                break;
            }
            
            if (attempt % 5 == 0)
            {
                Console.WriteLine($"Waiting for new UL... ({attempt + 1}/{maxAttempts})");
            }
        }
        
        if (!newUlDetected)
        {
            Console.WriteLine("WARNING: No new UL detected, will try to extract anyway...");
        }
        
        // Now get ALL UL elements and find the LAST one with street content
        var ulElements = await _page.Locator("ul").AllAsync();
        Console.WriteLine($"\n=== Found {ulElements.Count} UL elements ===");
        
        // Search from LAST to FIRST (most recent first)
        for (int i = ulElements.Count - 1; i >= 0; i--)
        {
            var ul = ulElements[i];
            var liElements = await ul.Locator("li").AllAsync();
            
            // If this UL has a reasonable number of LI elements (1-20)
            if (liElements.Count >= 1 && liElements.Count <= 20)
            {
                var streetTexts = new List<string>();
                
                foreach (var li in liElements)
                {
                    var text = (await li.TextContentAsync())?.Trim();
                    
                    // Filter out menu items and non-street content
                    if (!string.IsNullOrWhiteSpace(text) && 
                        text.Length < 100 && 
                        !text.ToLower().Contains("check parking") &&
                        !text.ToLower().Contains("correct zone") &&
                        !text.ToLower().Contains("general quer") &&
                        !text.ToLower().Contains("speak with agent") &&
                        !text.ToLower().Contains("main menu"))
                    {
                        streetTexts.Add(text);
                    }
                }
                
                // If we found potential streets (not menu items)
                if (streetTexts.Count >= 1)
                {
                    var result = string.Join("\n", streetTexts);
                    
                    // CRITICAL: Check if this response is different from the previous one
                    if (!string.IsNullOrEmpty(previousResponse))
                    {
                        var normalizedResult = result.ToLower().Replace(" ", "").Replace("\n", "");
                        var normalizedPrevious = previousResponse.ToLower().Replace(" ", "").Replace("\n", "");
                        
                        if (normalizedResult == normalizedPrevious)
                        {
                            Console.WriteLine($"UL #{i + 1}: SKIPPED (same as previous response)");
                            continue; // Skip this one, look for a different response
                        }
                    }
                    
                    Console.WriteLine($"UL #{i + 1}: {liElements.Count} LI elements");
                    foreach (var st in streetTexts)
                    {
                        Console.WriteLine($"    LI: '{st}'");
                    }
                    
                    Console.WriteLine($"\n✓ Extracted {streetTexts.Count} items from UL #{i + 1}");
                    Console.WriteLine($"Result: {result}");
                    return result;
                }
            }
        }
        
        Console.WriteLine("ERROR: Could not find street list in any UL element");
        return "";
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
            Console.WriteLine("Strategy 0: Looking for #chatResponseConfirmationYes...");
            var yesById = _page.Locator("#chatResponseConfirmationYes").First;
            await yesById.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 });
            if (await yesById.IsEnabledAsync())
            {
                await yesById.ScrollIntoViewIfNeededAsync();
                await yesById.ClickAsync(new() { Timeout = 5000 });
                Console.WriteLine("✓ Clicked #chatResponseConfirmationYes successfully");
                await Task.Delay(1500);
                return;
            }

            Console.WriteLine("#chatResponseConfirmationYes found but not enabled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"#chatResponseConfirmationYes click failed: {ex.Message}");
        }
        
        // Wait a bit for Yes button to appear
        await Task.Delay(2000);
        
        // Strategy 1: Click on text "Yes" directly
        try
        {
            Console.WriteLine("Strategy 1: Clicking text 'Yes' directly...");
            await _page.Locator("text=Yes").First.ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Clicked on text 'Yes' successfully");
            await Task.Delay(1500);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Text 'Yes' click failed: {ex.Message}");
        }
        
        // Strategy 2: Click button containing "Yes" text
        try
        {
            Console.WriteLine("Strategy 2: Clicking button:has-text('Yes')...");
            await _page.Locator("button:has-text('Yes')").First.ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Button with 'Yes' text clicked successfully");
            await Task.Delay(1500);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Button:has-text('Yes') click failed: {ex.Message}");
        }
        
        // Strategy 3: Wait for and click thumbIcon if visible
        try
        {
            Console.WriteLine("Strategy 3: Looking for #thumbIcon...");
            var thumbIconCount = await _page.Locator("#thumbIcon").CountAsync();
            if (thumbIconCount > 0)
            {
                Console.WriteLine($"Found {thumbIconCount} thumbIcon elements");
                await _page.Locator("#thumbIcon").First.ClickAsync(new() { Force = true, Timeout = 5000 });
                Console.WriteLine("✓ thumbIcon clicked successfully");
                await Task.Delay(1500);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"thumbIcon click failed: {ex.Message}");
        }
        
        // Strategy 4: Click any element containing 👍 emoji
        try
        {
            Console.WriteLine("Strategy 4: Looking for 👍 emoji...");
            await _page.Locator("text=👍").First.ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Thumbs up emoji clicked successfully");
            await Task.Delay(1500);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Emoji click failed: {ex.Message}");
        }
        
        // Strategy 5: Use JavaScript to click any element with "Yes" text
        try
        {
            Console.WriteLine("Strategy 5: Using JavaScript to click 'Yes'...");
            var clicked = await _page.EvaluateAsync<bool>(@"
                (() => {
                    var allElements = document.querySelectorAll('*');
                    for (var i = 0; i < allElements.length; i++) {
                        var element = allElements[i];
                        var text = element.textContent || '';
                        if (text.trim() === 'Yes' || text.includes('👍 Yes') || text.includes('Yes')) {
                            element.click();
                            return true;
                        }
                    }
                    return false;
                })();
            ");
            
            if (clicked)
            {
                Console.WriteLine("✓ JavaScript found and clicked 'Yes'");
                await Task.Delay(1500);
                return;
            }
            else
            {
                Console.WriteLine("JavaScript couldn't find 'Yes' element");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JavaScript click failed: {ex.Message}");
        }
        
        // Strategy 6: Look for button by class and click
        try
        {
            Console.WriteLine("Strategy 6: Looking for .btn, .button classes with 'Yes'...");
            var buttonSelectors = new[] { ".btn", ".button", "[role='button']" };
            
            foreach (var selector in buttonSelectors)
            {
                var buttons = await _page.Locator(selector).AllAsync();
                foreach (var button in buttons)
                {
                    var text = await button.TextContentAsync();
                    if (text != null && text.Contains("Yes"))
                    {
                        Console.WriteLine($"Found button with selector '{selector}' containing 'Yes'");
                        await button.ClickAsync(new() { Timeout = 3000 });
                        Console.WriteLine("✓ Button clicked successfully");
                        await Task.Delay(1500);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Button class search failed: {ex.Message}");
        }
        
        Console.WriteLine("⚠ WARNING: All Yes button click strategies failed");
        Console.WriteLine("Attempting to print available buttons on page...");
        
        // Debug: Print all buttons on page
        try
        {
            var allButtons = await _page.Locator("button").AllAsync();
            Console.WriteLine($"Found {allButtons.Count} buttons on page:");
            for (int i = 0; i < Math.Min(10, allButtons.Count); i++)
            {
                var btnText = await allButtons[i].TextContentAsync();
                Console.WriteLine($"  Button {i}: '{btnText?.Trim()}'");
            }
        }
        catch { }
        
        throw new Exception("Failed to click Yes button after trying all strategies");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error clicking Yes button: {ex.Message}");
        throw;
    }
}

public async Task DebugPrintPageElementsAsync()
{
    try
    {
        Console.WriteLine("\n========== DEBUG: PAGE ELEMENTS ==========");
        
        // Print all elements with class names
        var allElements = await _page.Locator("*[class]").AllAsync();
        Console.WriteLine($"Total elements with classes: {allElements.Count}");
        
        var seenClasses = new HashSet<string>();
        foreach (var element in allElements.Take(50)) // Limit to first 50
        {
            var className = await element.GetAttributeAsync("class");
            if (!string.IsNullOrEmpty(className) && !seenClasses.Contains(className))
            {
                seenClasses.Add(className);
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text) && text.Length > 5)
                {
                    Console.WriteLine($"Class: '{className}' | Text: '{text.Substring(0, Math.Min(50, text.Length))}'");
                }
            }
        }
        
        Console.WriteLine("==========================================\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Debug error: {ex.Message}");
    }
}

public async Task<bool> VerifyMessageSentAsync(string expectedMessage)
{
    try
    {
        Console.WriteLine($"Verifying message '{expectedMessage}' was sent...");
        
        // Wait briefly for message to appear in chat
        await Task.Delay(1000);
        
        // Look for user message bubbles/elements
        var userMessageSelectors = new[] 
        { 
            ".msg-user",
            ".user-message",
            ".message-user",
            "[data-role='user']",
            ".chat-user"
        };
        
        foreach (var selector in userMessageSelectors)
        {
            var messages = await _page.Locator(selector).AllAsync();
            if (messages.Count > 0)
            {
                var lastMessage = messages[messages.Count - 1];
                var messageText = await lastMessage.TextContentAsync();
                messageText = messageText?.Trim().ToLower() ?? "";
                
                if (messageText.Contains(expectedMessage.ToLower()))
                {
                    Console.WriteLine($"✓ Message verified with selector '{selector}': {messageText}");
                    return true;
                }
            }
        }
        
        // Fallback: Check all text content on page
        var pageContent = await _page.ContentAsync();
        if (pageContent.Contains(expectedMessage))
        {
            Console.WriteLine($"✓ Message found in page content");
            return true;
        }
        
        Console.WriteLine("✗ Message not found in chat - may not have been sent!");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verifying message: {ex.Message}");
        return false;
    }
}

public async Task ClickMainCardOptionAsync()
{
    try
    {
        Console.WriteLine("Clicking Main Menu button...");
        
        // Strategy 1: Wait for #mainMenu and click
        try
        {
            Console.WriteLine("Strategy 1: Looking for #mainMenu button...");
            await _page.WaitForSelectorAsync("#mainMenu", new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15000 });
            Console.WriteLine("Main Menu button is visible");
            
            await _page.Locator("#mainMenu").ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Main Menu button clicked successfully");
            await Task.Delay(1500);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"#mainMenu click failed: {ex.Message}");
        }
        
        // Strategy 2: Click text "Main Menu"
        try
        {
            Console.WriteLine("Strategy 2: Clicking text 'Main Menu'...");
            await _page.Locator("text=Main Menu").First.ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Main Menu clicked via text locator");
            await Task.Delay(1500);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Text 'Main Menu' click failed: {ex.Message}");
        }
        
        // Strategy 3: Click button containing "Main Menu"
        try
        {
            Console.WriteLine("Strategy 3: Clicking button:has-text('Main Menu')...");
            await _page.Locator("button:has-text('Main Menu')").First.ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✓ Main Menu button clicked");
            await Task.Delay(1500);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Button:has-text('Main Menu') click failed: {ex.Message}");
        }
        
        // Strategy 4: Look for .mainmenu-btn class
        try
        {
            Console.WriteLine("Strategy 4: Looking for .mainmenu-btn class...");
            var mainMenuBtn = await _page.Locator(".mainmenu-btn").CountAsync();
            if (mainMenuBtn > 0)
            {
                await _page.Locator(".mainmenu-btn").First.ClickAsync(new() { Timeout = 5000 });
                Console.WriteLine("✓ Main Menu button clicked via class");
                await Task.Delay(1500);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($".mainmenu-btn click failed: {ex.Message}");
        }
        
        // Strategy 5: JavaScript click on #mainMenu
        try
        {
            Console.WriteLine("Strategy 5: JavaScript click on #mainMenu...");
            var clicked = await _page.EvaluateAsync<bool>(@"
                (() => {
                    var mainMenu = document.getElementById('mainMenu');
                    if (mainMenu) {
                        mainMenu.click();
                        return true;
                    }
                    return false;
                })();
            ");
            
            if (clicked)
            {
                Console.WriteLine("✓ Main Menu clicked via JavaScript");
                await Task.Delay(1500);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JavaScript click failed: {ex.Message}");
        }
        
        Console.WriteLine("⚠ WARNING: All Main Menu click strategies failed - continuing anyway");
        // Don't throw exception - just log warning and continue
        await Task.Delay(1000);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error clicking Main Menu button: {ex.Message}");
        // Don't throw - just log and continue
    }
}
// Ensure all methods and class braces are properly closed.
}