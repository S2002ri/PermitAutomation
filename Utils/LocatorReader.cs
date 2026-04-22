using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json; // For JSON deserialization

namespace PlaywrightAutomation.Utils
{
    public class LocatorReader
    {
        private readonly Dictionary<string, string> _locators;

        public LocatorReader()
        {
            _locators = new Dictionary<string, string>
            {
                { "ChatBotIcon", "img.chatbot-btn-avatar" },
                { "FirstName", "#user-first-name" },
                { "LastName", "#user-lastname" },
                { "MobileNo", "#user-mobilenumber" },
                { "SubmitButton", "#btn-user-submit" },
                { "ChatbotMenuOption", "#menuCard0" },
                { "ChatInputBox", "#txt_Chat" },
                { "SendButton", "#btnImage" }
            };
        }

        public LocatorReader(string filePath)
        {
            _locators = new Dictionary<string, string>(); // Ensure _locators is always initialized

            if (File.Exists(filePath))
            {
                try
                {
                    Console.WriteLine($"Reading locators from file: {filePath}");
                    var jsonContent = File.ReadAllText(filePath);
                    Console.WriteLine($"File content: {jsonContent}");

                    var loadedLocators = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

                    if (loadedLocators != null)
                    {
                        _locators = loadedLocators;
                        Console.WriteLine("Locators loaded successfully:");
                        foreach (var locator in _locators)
                        {
                            Console.WriteLine($"Key: {locator.Key}, Value: {locator.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No locators found in the file.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading locators: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Locator file not found: {filePath}");
            }
        }

        public string GetLocator(string elementName)
        {
            return _locators.TryGetValue(elementName, out var value) ? value : string.Empty;
        }
    }
}
