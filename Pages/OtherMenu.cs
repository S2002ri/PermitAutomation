using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Pages
{
    public class OtherMenu
    {
        private readonly IPage _page;

        public OtherMenu(IPage page)
        {
            _page = page;
        }

        /// <summary>
        /// Clicks the main menu card whose visible text matches <paramref name="menuName"/>.
        /// </summary>
        public async Task ClickMenuCardAsync(string menuName)
        {
            Console.WriteLine($"  [OtherMenu] Clicking main card: '{menuName}'");

            var selectors = new[]
            {
                $"button:has-text('{menuName}')",
                $"[data-template-name='{menuName}']",
                $"text={menuName}",
                $".msg-ans-btn:has-text('{menuName}')",
                $"button.btn:has-text('{menuName}')"
            };

            Exception? lastEx = null;
            foreach (var selector in selectors)
            {
                try
                {
                    // Use .Last — targets the most recently rendered card, not a historical one in the chat log.
                    var all = _page.Locator(selector);
                    await all.Last.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
                    await all.Last.ScrollIntoViewIfNeededAsync();
                    await all.Last.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(1500);
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            throw new Exception($"Unable to click main menu card '{menuName}'.", lastEx);
        }

        /// <summary>
        /// Clicks the sub-card (submenu option) whose visible text matches <paramref name="subMenuName"/>.
        /// Sub-cards are expected to be visible after the main card has been clicked.
        /// </summary>
        public async Task ClickSubMenuCardAsync(string subMenuName)
        {
            Console.WriteLine($"  [OtherMenu] Clicking sub-card: '{subMenuName}'");

            var selectors = new[]
            {
                $"button:has-text('{subMenuName}')",
                $"[data-template-name='{subMenuName}']",
                $"text={subMenuName}",
                $".msg-ans-btn:has-text('{subMenuName}')",
                $"button.btn:has-text('{subMenuName}')"
            };

            Exception? lastEx = null;
            foreach (var selector in selectors)
            {
                try
                {
                    // Use .Last — newest sub-card, not a stale historical button.
                    var all = _page.Locator(selector);
                    await all.Last.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
                    await all.Last.ScrollIntoViewIfNeededAsync();
                    await all.Last.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(1500);
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            throw new Exception($"Unable to click sub-menu card '{subMenuName}'.", lastEx);
        }

        /// <summary>
        /// Captures all currently visible bot message texts as a snapshot (to diff against later).
        /// </summary>
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
                    var allTexts = await _page.Locator(selector).AllTextContentsAsync();
                    foreach (var text in allTexts)
                    {
                        var n = NormalizeResponse(text?.Trim() ?? "");
                        if (!string.IsNullOrWhiteSpace(n))
                            snapshot.Add(n);
                    }
                }
                catch { /* ignore */ }
            }

            return snapshot;
        }

        /// <summary>
        /// Polls for a new, non-boilerplate bot response that did not exist in <paramref name="previousTexts"/>.
        /// Waits up to 30 seconds. Used for GQ bridge responses.
        /// </summary>
        public async Task<string> GetResponseTextAsync(HashSet<string> previousTexts)
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

            for (int attempt = 0; attempt < 30; attempt++)
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

                            var normalized = NormalizeResponse(raw);
                            if (string.IsNullOrWhiteSpace(normalized)) continue;
                            if (previousTexts.Contains(normalized)) continue;
                            if (IsBoilerplateResponse(normalized)) continue;

                            Console.WriteLine($"  -> Bot response captured (attempt {attempt + 1})");
                            return raw;
                        }
                    }
                    catch { /* try next selector */ }
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("  -> WARNING: No real bot response found after 30s");
            return string.Empty;
        }

        /// <summary>
        /// Waits up to 40 seconds for the actual substantive bot response after a submenu
        /// card is clicked. Walks newest-first so that boilerplate follow-up messages
        /// ("Is there anything else…?") are skipped and only the real content is returned.
        /// </summary>
        public async Task<string> GetSubMenuResponseAsync(HashSet<string> snapshotBeforeSubmenu)
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

            for (int attempt = 0; attempt < 40; attempt++)
            {
                foreach (var selector in selectors)
                {
                    try
                    {
                        var allTexts = await _page.Locator(selector).AllTextContentsAsync();

                        // Collect all NEW non-boilerplate messages (newest to oldest)
                        // so that a boilerplate follow-up ("Is there anything else…?")
                        // that appears AFTER the real answer does not shadow it.
                        string? bestCandidate = null;

                        for (int i = allTexts.Count - 1; i >= 0; i--)
                        {
                            var raw = allTexts[i]?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw)) continue;

                            var normalized = NormalizeResponse(raw);
                            if (string.IsNullOrWhiteSpace(normalized)) continue;

                            // Must be truly new (not seen before submenu click)
                            if (snapshotBeforeSubmenu.Contains(normalized)) continue;

                            if (IsBoilerplateResponse(normalized))
                            {
                                // It's a new message but boilerplate – skip it,
                                // but keep scanning older messages for the real answer.
                                continue;
                            }

                            // First non-boilerplate new message walking newest→oldest
                            // is the actual submenu response.
                            bestCandidate = raw;
                            break;
                        }

                        if (bestCandidate != null)
                        {
                            Console.WriteLine($"  -> Submenu response captured (attempt {attempt + 1})");
                            return bestCandidate;
                        }
                    }
                    catch { /* try next selector */ }
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("  -> WARNING: No submenu bot response found after 40s");
            return string.Empty;
        }

        /// <summary>
        /// Clicks the MOST RECENT "Yes" confirmation button (always .Last to avoid stale
        /// historical buttons that have already been answered in earlier rows).
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
                    if (await all.CountAsync() == 0)
                        continue;

                    // .Last targets the most recently rendered Yes button — never a stale one
                    await all.Last.ScrollIntoViewIfNeededAsync();
                    await all.Last.ClickAsync(new() { Timeout = 4000 });
                    await Task.Delay(700);
                    return;
                }
                catch { /* try next */ }
            }
        }

        /// <summary>
        /// Clicks the General Queries main card to enter the GQ flow.
        /// </summary>
        public async Task ClickGeneralQueriesCardAsync()
        {
            Console.WriteLine("  [OtherMenu] Clicking General Queries card");
            var selectors = new[]
            {
                "#menuCard3",
                "button:has-text('General Queries')",
                "button.btn.msg-ans-btn[data-template-name=\"General Queries\"]",
                "text=General Queries"
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
                    await Task.Delay(1000);
                    return;
                }
                catch (Exception ex) { lastEx = ex; }
            }
            throw new Exception("Unable to click General Queries card.", lastEx);
        }

        /// <summary>
        /// Types a question into the chat input and clicks Send.
        /// </summary>
        public async Task TypeAndSendAsync(string question)
        {
            Console.WriteLine($"  [OtherMenu] Typing: '{question}'");
            var inputSelectors = new[] { "#txt_Chat", "textarea.msg-input-area", "textarea", "input[type='text']" };
            foreach (var sel in inputSelectors)
            {
                try
                {
                    var loc = _page.Locator(sel).First;
                    if (await loc.CountAsync() == 0) continue;
                    await loc.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 });
                    await loc.ClickAsync(new() { Timeout = 3000 });
                    await loc.FillAsync(question);
                    break;
                }
                catch { }
            }

            // Send
            var sendSelectors = new[] { "#btn-user-submit", "button.btn-send", "button[type='submit']", ".msg-send-btn", "button:has(svg)" };
            foreach (var sel in sendSelectors)
            {
                try
                {
                    var loc = _page.Locator(sel).Last;
                    if (await _page.Locator(sel).CountAsync() == 0) continue;
                    await loc.ClickAsync(new() { Timeout = 5000 });
                    await Task.Delay(500);
                    return;
                }
                catch { }
            }
            await _page.Locator("#txt_Chat").PressAsync("Enter");
            await Task.Delay(500);
        }

        /// <summary>
        /// Navigates back to the main menu card list.
        /// </summary>
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
                catch { /* try next */ }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        private static bool IsBoilerplateResponse(string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized))
                return true;

            var patterns = new[]
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

            foreach (var p in patterns)
                if (normalized.Contains(p)) return true;

            // No word-count floor here — sub-menu responses are often short and must not be discarded.
            return false;
        }

        private static string NormalizeResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var n = text.ToLowerInvariant().Replace("\r", " ").Replace("\n", " ");
            n = Regex.Replace(n, @"[^\p{L}\p{Nd}\s]", " ");
            n = Regex.Replace(n, @"\s+", " ").Trim();
            return n;
        }

        /// <summary>
        /// Polls up to 40 seconds and returns the most recent non-boilerplate bot message
        /// visible in the chat — no snapshot comparison, just the latest response.
        /// </summary>
        public async Task<string> GetLatestBotResponseAsync()
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

            for (int attempt = 0; attempt < 40; attempt++)
            {
                foreach (var selector in selectors)
                {
                    try
                    {
                        var allTexts = await _page.Locator(selector).AllTextContentsAsync();

                        // Walk newest → oldest; return first non-boilerplate message found
                        for (int i = allTexts.Count - 1; i >= 0; i--)
                        {
                            var raw = allTexts[i]?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw)) continue;

                            var normalized = NormalizeResponse(raw);
                            if (string.IsNullOrWhiteSpace(normalized)) continue;
                            if (IsBoilerplateResponse(normalized)) continue;

                            Console.WriteLine($"  -> Latest bot response captured (attempt {attempt + 1})");
                            return raw;
                        }
                    }
                    catch { /* try next selector */ }
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("  -> WARNING: No bot response found after 40s");
            return string.Empty;
        }

        /// <summary>
        /// Returns the current count of visible main-menu / submenu choice buttons.
        /// Used to verify the main cards have reappeared after a Yes reset.
        /// </summary>
        public async Task<int> GetMenuButtonCountAsync()
        {
            try { return await _page.Locator("button.btn.msg-ans-btn").CountAsync(); }
            catch { return 0; }
        }

        /// <summary>
        /// Returns the current number of bot message list items in the chat.
        /// Used as a positional baseline — only messages at index ≥ this value are treated as new.
        /// </summary>
        public async Task<int> GetBotMessageCountAsync()
        {
            var selectors = new[] { "li.botMsgLt", "#chatSection li.botMsgLt" };
            foreach (var sel in selectors)
            {
                try
                {
                    int c = await _page.Locator(sel).CountAsync();
                    if (c > 0) return c;
                }
                catch { }
            }
            return 0;
        }

        /// <summary>
        /// Polls up to 40 s for a non-boilerplate bot message that appears at index >= countBefore.
        /// Index-based, NOT text-based — the same response text appearing twice is always treated
        /// as new, so repeated answers are never silently dropped.
        /// </summary>
        public async Task<string> GetNewBotMessageAsync(int countBefore)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                try
                {
                    var items = _page.Locator("li.botMsgLt");
                    int total = await items.CountAsync();

                    if (total > countBefore)
                    {
                        // Walk newest-first through only the NEW messages
                        for (int i = total - 1; i >= countBefore; i--)
                        {
                            string raw;
                            try { raw = (await items.Nth(i).TextContentAsync())?.Trim() ?? string.Empty; }
                            catch { continue; }

                            if (string.IsNullOrWhiteSpace(raw)) continue;
                            var normalized = NormalizeResponse(raw);
                            if (string.IsNullOrWhiteSpace(normalized)) continue;
                            if (IsBoilerplateResponse(normalized)) continue;

                            Console.WriteLine($"  -> Message at index {i}/{total} captured (attempt {attempt + 1})");
                            return raw;
                        }
                    }
                }
                catch { }

                await Task.Delay(1000);
            }

            Console.WriteLine("  -> WARNING: No new non-boilerplate message found after 40s");
            return string.Empty;
        }
    }
}
