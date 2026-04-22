using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Pages
{
    public class GeneralQueris
    {
        private readonly IPage _page;

        public GeneralQueris(IPage page)
        {
            _page = page;
        }

        public async Task OpenCardAsync()
        {
            var selectors = new[]
            {
                "#menuCard3",
                "button:has-text('General Queries')",
                "button.btn.msg-ans-btn[data-template-name=\"General Queries\"]",
                "text=General Queries"
            };

            Exception? lastException = null;
            foreach (var selector in selectors.Distinct())
            {
                try
                {
                    var locator = _page.Locator(selector).First;
                    await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                    await locator.ScrollIntoViewIfNeededAsync();
                    await locator.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(1000);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            throw new Exception("Unable to click General Queries card.", lastException);
        }

        public async Task TypeQuestionAsync(string question)
        {
            var textFieldSelectors = new[] { "#txt_Chat", "textarea.msg-input-area", "textarea", "input[type='text']" };

            foreach (var selector in textFieldSelectors)
            {
                try
                {
                    var locator = _page.Locator(selector).First;
                    if (await locator.CountAsync() == 0)
                        continue;

                    await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 });
                    await locator.ClickAsync(new() { Timeout = 3000 });
                    await locator.FillAsync(question);
                    return;
                }
                catch
                {
                    // Try next selector
                }
            }

            var serializedQuestion = JsonSerializer.Serialize(question);
            await _page.EvaluateAsync($@"
                (() => {{
                    const textbox = document.getElementById('txt_Chat') || document.querySelector('textarea') || document.querySelector('input[type=""text""]');
                    if (textbox) {{
                        textbox.disabled = false;
                        textbox.value = {serializedQuestion};
                        textbox.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        textbox.dispatchEvent(new Event('change', {{ bubbles: true }}));
                    }}
                }})();
            ");
        }

        public async Task ClickSendButtonAsync()
        {
            var selectors = new[]
            {
                "#btn-user-submit",
                "button.btn-send",
                "button[type='submit']",
                ".msg-send-btn",
                "button:has(svg)"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var locator = _page.Locator(selector).Last;
                    if (await _page.Locator(selector).CountAsync() == 0)
                        continue;

                    await locator.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(500);
                    return;
                }
                catch
                {
                    // Try next selector
                }
            }

            await _page.Locator("#txt_Chat").PressAsync("Enter");
            await Task.Delay(500);
        }

        public async Task<HashSet<string>> GetBotMessageTextsSnapshotAsync()
        {
            var snapshot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

            foreach (var selector in selectors)
            {
                try
                {
                    var locator = _page.Locator(selector);
                    var allTexts = await locator.AllTextContentsAsync();
                    foreach (var text in allTexts)
                    {
                        var normalized = NormalizeResponse(text?.Trim() ?? "");
                        if (!string.IsNullOrWhiteSpace(normalized))
                            snapshot.Add(normalized);
                    }
                }
                catch
                {
                    // Ignore selector failures
                }
            }

            return snapshot;
        }

        public async Task<string> GetResponseTextAsync(HashSet<string> previousMessageTexts)
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

            // Poll up to 30 seconds (30 x 1s) for a real (non-boilerplate) bot answer
            for (int attempt = 0; attempt < 30; attempt++)
            {
                foreach (var selector in selectors)
                {
                    try
                    {
                        var locator = _page.Locator(selector);
                        var allTexts = await locator.AllTextContentsAsync();

                        // Iterate newest-first to find the latest real answer
                        for (int i = allTexts.Count - 1; i >= 0; i--)
                        {
                            var raw = allTexts[i]?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw))
                                continue;

                            var normalizedCurrent = NormalizeResponse(raw);
                            if (string.IsNullOrWhiteSpace(normalizedCurrent))
                                continue;

                            // Skip if this text existed before we sent the question
                            if (previousMessageTexts.Contains(normalizedCurrent))
                                continue;

                            // Skip boilerplate / menu prompts — keep waiting
                            if (IsBoilerplateResponse(normalizedCurrent))
                                continue;

                            // Found a genuinely new, non-boilerplate answer
                            Console.WriteLine($"  -> Bot response captured (attempt {attempt + 1})");
                            return raw;
                        }
                    }
                    catch
                    {
                        // Try next selector
                    }
                }

                await Task.Delay(1000);
            }

            // Exhausted retries — no new response found
            Console.WriteLine("  -> WARNING: No real bot response found after 30s");
            return string.Empty;
        }

        private static bool IsBoilerplateResponse(string normalizedResponse)
        {
            if (string.IsNullOrWhiteSpace(normalizedResponse))
                return true;

            var boilerplatePatterns = new[]
            {
                "hi im pam",
                "virtual assistant",
                "here are some common questions about",
                "is there anything else i can help you with",
                "anything else i can help",
                "i can help you",
                "how can i help",
                "what can i help",
                "please choose from the options below",
                "please choose from the categories below",
                "please enter your question",
                "please select from the options",
                "please select from the categories",
                "choose from the options",
                "choose from the categories",
                "select an option",
                "check parking streets",
                "correct zone",
                "general queries",
                "speak with agent",
                "main menu",
                "loading",
                "is there anything else",
                "can i help you with",
                "assist you with"
            };

            foreach (var pattern in boilerplatePatterns)
            {
                if (normalizedResponse.Contains(pattern))
                    return true;
            }

            // If the entire response is very short (under 12 words), it's likely a suggestion/prompt
            int wordCount = normalizedResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount <= 10)
                return true;

            return false;
        }

        private static string NormalizeResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.ToLowerInvariant().Replace("\r", " ").Replace("\n", " ");
            normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }

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
                    if (await _page.Locator(selector).CountAsync() == 0)
                        continue;

                    await _page.Locator(selector).First.ClickAsync(new() { Timeout = 4000 });
                    await Task.Delay(700);
                    return;
                }
                catch
                {
                    // Try next selector
                }
            }
        }

        public async Task ClickMainCardOptionAsync()
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
                    if (await _page.Locator(selector).CountAsync() == 0)
                        continue;

                    await _page.Locator(selector).First.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(1000);
                    return;
                }
                catch
                {
                    // Try next selector
                }
            }
        }
    }
}