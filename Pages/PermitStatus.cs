using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Pages
{
    public class PermitStatus
    {
        private readonly IPage _page;

        public PermitStatus(IPage page)
        {
            _page = page;
        }

        /// <summary>
        /// Clicks the "Check Permit Status" main card button.
        /// </summary>
        public async Task ClickCheckPermitStatusCardAsync()
        {
            Console.WriteLine("  [PermitStatus] Clicking Check Permit Status card");

            var selectors = new[]
            {
                "button:has-text('Check Permit Status')",
                "[data-template-name='Check Permit Status']",
                "text=Check Permit Status",
                ".msg-ans-btn:has-text('Check Permit Status')",
                "button.btn:has-text('Check Permit Status')"
            };

            Exception? lastEx = null;
            foreach (var selector in selectors)
            {
                try
                {
                    var all = _page.Locator(selector);
                    await all.Last.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                    await all.Last.ScrollIntoViewIfNeededAsync();
                    await all.Last.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(1500);
                    return;
                }
                catch (Exception ex) { lastEx = ex; }
            }

            throw new Exception("Unable to click Check Permit Status card.", lastEx);
        }

        /// <summary>
        /// Clears the Last Name, Post Code and Permit Number fields for the given form index.
        /// Row 1 = formIndex 1 => #lastname1, #postcode1, #permitnumber1
        /// Row 2 = formIndex 2 => #lastname2, #postcode2, #permitnumber2  (and so on)
        /// </summary>
        public async Task ClearFormFieldsAsync(int formIndex)
        {
            Console.WriteLine($"  [PermitStatus] Clearing form fields (index {formIndex})");
            var ids = new[] { $"lastname{formIndex}", $"postcode{formIndex}", $"permitnumber{formIndex}" };

            foreach (var id in ids)
            {
                try
                {
                    var loc = _page.Locator($"#{id}");
                    if (await loc.CountAsync() == 0) continue;
                    await loc.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                    await loc.First.FillAsync("");
                    Console.WriteLine($"  [PermitStatus] #{id} cleared");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [PermitStatus] Warning: could not clear #{id}: {ex.Message}");
                }
            }
            await Task.Delay(200);
        }

        /// <summary>
        /// Enters the Last Name using the form-index-specific field id.
        /// </summary>
        public async Task EnterLastNameAsync(string lastName, int formIndex)
        {
            Console.WriteLine($"  [PermitStatus] Entering Last Name: '{lastName}' (form {formIndex})");
            await FillFieldByIdAsync($"lastname{formIndex}", lastName);
        }

        /// <summary>
        /// Enters the Post Code using the form-index-specific field id.
        /// </summary>
        public async Task EnterPostCodeAsync(string postCode, int formIndex)
        {
            Console.WriteLine($"  [PermitStatus] Entering Post Code: '{postCode}' (form {formIndex})");
            await FillFieldByIdAsync($"postcode{formIndex}", postCode);
        }

        /// <summary>
        /// Enters the Permit Number using the form-index-specific field id.
        /// </summary>
        public async Task EnterPermitNumberAsync(string permitNumber, int formIndex)
        {
            Console.WriteLine($"  [PermitStatus] Entering Permit Number: '{permitNumber}' (form {formIndex})");
            await FillFieldByIdAsync($"permitnumber{formIndex}", permitNumber);
        }

        /// <summary>
        /// Clicks the SUBMIT button scoped to the active form (identified by the form index).
        /// </summary>
        public async Task ClickSubmitButtonAsync(int formIndex)
        {
            Console.WriteLine($"  [PermitStatus] Clicking SUBMIT button (form {formIndex})");

            // Primary: exact indexed button id
            var primaryId = $"#permitStatus{formIndex}";
            try
            {
                var btn = _page.Locator(primaryId);
                await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                await btn.ScrollIntoViewIfNeededAsync();
                await btn.ClickAsync(new() { Timeout = 5000 });
                await Task.Delay(500);
                Console.WriteLine($"  [PermitStatus] Submit clicked via '{primaryId}'");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [PermitStatus] Primary submit id failed: {ex.Message}");
            }

            // Fallback: last visible Submit/SUBMIT button on page
            foreach (var sel in new[] { "button:has-text('Submit')", "button:has-text('SUBMIT')", "input[type='submit']", "button[type='submit']" })
            {
                try
                {
                    var loc = _page.Locator(sel);
                    if (await loc.CountAsync() == 0) continue;
                    await loc.Last.ScrollIntoViewIfNeededAsync();
                    await loc.Last.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(500);
                    Console.WriteLine($"  [PermitStatus] Submit clicked via fallback '{sel}'");
                    return;
                }
                catch { }
            }

            throw new Exception($"Unable to click SUBMIT button for form index {formIndex}.");
        }

        /// <summary>
        /// Waits up to 60 seconds for a structured permit status response and returns the full text.
        /// Matches responses containing permit-specific fields like "Current Status", "Permit Category", etc.
        /// </summary>
        public async Task<string> GetPermitStatusResponseAsync()
        {
            var selectors = new[]
            {
                "li.botMsgLt .msgBubble",
                "#chatSection li.botMsgLt",
                "li.botMsgLt",
                ".bot-response",
                ".message-content",
                ".msg-bot",
                ".msgBubble",
                "#chatSection li",
                "[class*='bot']",
                "[class*='response']"
            };

            for (int attempt = 0; attempt < 60; attempt++)
            {
                foreach (var selector in selectors)
                {
                    try
                    {
                        var allTexts = await _page.Locator(selector).AllTextContentsAsync();
                        for (int i = allTexts.Count - 1; i >= 0; i--)
                        {
                            var raw = allTexts[i]?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw)) continue;

                            if (raw.Contains("Current Status", StringComparison.OrdinalIgnoreCase) ||
                                raw.Contains("Permit Category", StringComparison.OrdinalIgnoreCase) ||
                                raw.Contains("Permit Expiry", StringComparison.OrdinalIgnoreCase) ||
                                raw.Contains("Status Description", StringComparison.OrdinalIgnoreCase) ||
                                raw.Contains("no permit found", StringComparison.OrdinalIgnoreCase) ||
                                raw.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                raw.Contains("no record", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"  -> Permit status response found (attempt {attempt + 1})");
                                return raw;
                            }
                        }
                    }
                    catch { }
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("  -> WARNING: No permit status response found after 60s");
            return string.Empty;
        }

        /// <summary>
        /// Clicks the most recent Yes button.
        /// </summary>
        public async Task ClickYesButtonAsync()
        {
            var selectors = new[]
            {
                "#chatResponseConfirmationYes",
                "button:has-text('Yes')",
                "text=/^\\s*Yes\\s*$/"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var all = _page.Locator(selector);
                    if (await all.CountAsync() == 0) continue;
                    await all.Last.ScrollIntoViewIfNeededAsync();
                    await all.Last.ClickAsync(new() { Timeout = 4000 });
                    await Task.Delay(700);
                    return;
                }
                catch { }
            }
        }

        /// <summary>
        /// Navigates back to the main menu.
        /// </summary>
        public async Task ClickMainMenuAsync()
        {
            var selectors = new[]
            {
                "#mainMenu",
                "button:has-text('Main Menu')",
                "text=Main Menu"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    if (await _page.Locator(selector).CountAsync() == 0) continue;
                    await _page.Locator(selector).First.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(1000);
                    return;
                }
                catch { }
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Fills a field by its exact element id. Clears first, then types.
        /// </summary>
        private async Task FillFieldByIdAsync(string id, string value)
        {
            var loc = _page.Locator($"#{id}");

            if (await loc.CountAsync() == 0)
                throw new Exception($"Field #{id} not found on page.");

            await loc.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });

            // Clear then type
            await loc.First.FillAsync("");
            await loc.First.PressSequentiallyAsync(value, new() { Delay = 50 });

            // Verify
            var current = await loc.First.InputValueAsync();
            if (!current.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                // JS fallback using the element handle directly
                await loc.First.EvaluateAsync(
                    @"(el, val) => {
                        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')?.set;
                        if (setter) setter.call(el, val); else el.value = val;
                        el.dispatchEvent(new Event('input',  { bubbles: true }));
                        el.dispatchEvent(new Event('change', { bubbles: true }));
                    }",
                    value);
                Console.WriteLine($"  [PermitStatus] JS set #{id} = '{value}'");
            }
            else
            {
                Console.WriteLine($"  [PermitStatus] #{id} = '{value}'");
            }
        }
    }
}
