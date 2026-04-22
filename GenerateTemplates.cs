using System;
using System.Threading.Tasks;
using PlaywrightAutomation.Utils;

namespace PlaywrightAutomation
{
    /// <summary>
    /// Standalone utility to generate Excel template files for all councils
    /// Run this before executing tests to ensure all council Excel files exist
    /// </summary>
    public class GenerateTemplates
    {
        public static void Generate(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Excel Template Generator for Councils");
            Console.WriteLine("========================================\n");

            try
            {
                // Load configuration
                var configManager = ConfigManager.Instance;
                configManager.LoadConfig("config.json");

                // Generate all templates
                ExcelTemplateGenerator.CreateAllCouncilTemplates();

                Console.WriteLine("\n========================================");
                Console.WriteLine("Template generation completed successfully!");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
