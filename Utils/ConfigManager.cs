using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PlaywrightAutomation.Utils
{
    /// <summary>
    /// User details configuration
    /// </summary>
    public class UserDetails
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Council file paths configuration
    /// </summary>
    public class CouncilFiles
    {
        public string Permit { get; set; } = string.Empty;
        public string Zones { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Queries { get; set; } = string.Empty;
        public string TestData { get; set; } = string.Empty;
        public string TestCases { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual council configuration
    /// </summary>
    public class CouncilConfig
    {
        public string Name { get; set; } = string.Empty;
        public bool Run { get; set; } = false;
        public string Url { get; set; } = string.Empty;
        public CouncilFiles Files { get; set; } = new CouncilFiles();
    }

    /// <summary>
    /// Root configuration class
    /// </summary>
    public class AutomationConfig
    {
        public UserDetails UserDetails { get; set; } = new UserDetails();
        public List<CouncilConfig> Councils { get; set; } = new List<CouncilConfig>();
    }

    /// <summary>
    /// Manages configuration loading and access for multi-council automation
    /// </summary>
    public class ConfigManager
    {
        private static ConfigManager? _instance;
        private static readonly object _lock = new object();
        private AutomationConfig? _config;
        private CouncilConfig? _activeCouncil;

        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ConfigManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Load configuration from config.json file
        /// </summary>
        public void LoadConfig(string configPath = "config.json")
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {configPath}");
                }

                string jsonContent = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<AutomationConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (_config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration file.");
                }

                // Find the active council (run = true)
                _activeCouncil = _config.Councils.FirstOrDefault(c => c.Run);

                if (_activeCouncil == null)
                {
                    Console.WriteLine("WARNING: No council has 'run' set to true. No tests will be executed.");
                }
                else
                {
                    Console.WriteLine($"Active Council: {_activeCouncil.Name}");
                    Console.WriteLine($"URL: {_activeCouncil.Url}");
                    Console.WriteLine($"Permit File: {_activeCouncil.Files.Permit}");
                    Console.WriteLine($"Zones File: {_activeCouncil.Files.Zones}");
                    Console.WriteLine($"Status File: {_activeCouncil.Files.Status}");
                    Console.WriteLine($"Queries File: {_activeCouncil.Files.Queries}");
                }

                Console.WriteLine("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the active council configuration (where run = true)
        /// </summary>
        public CouncilConfig? GetActiveCouncil()
        {
            return _activeCouncil;
        }

        /// <summary>
        /// Get user details from configuration
        /// </summary>
        public UserDetails GetUserDetails()
        {
            if (_config?.UserDetails == null)
            {
                throw new InvalidOperationException("User details not loaded. Call LoadConfig first.");
            }
            return _config.UserDetails;
        }

        /// <summary>
        /// Get the URL for the active council
        /// </summary>
        public string GetActiveCouncilUrl()
        {
            if (_activeCouncil == null)
            {
                throw new InvalidOperationException("No active council found. Ensure one council has 'run' set to true.");
            }
            return _activeCouncil.Url;
        }

        /// <summary>
        /// Get file path for a specific file type from the active council
        /// </summary>
        public string GetFilePath(string fileType)
        {
            if (_activeCouncil?.Files == null)
            {
                throw new InvalidOperationException("No active council found. Ensure one council has 'run' set to true.");
            }

            string path = fileType.ToLower() switch
            {
                "permit" => _activeCouncil.Files.Permit,
                "zones" => _activeCouncil.Files.Zones,
                "status" => _activeCouncil.Files.Status,
                "queries" => _activeCouncil.Files.Queries,
                "testdata" => _activeCouncil.Files.TestData,
                "testcases" => _activeCouncil.Files.TestCases,
                _ => throw new ArgumentException($"Unknown file type: {fileType}")
            };

            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException($"File path for '{fileType}' is not configured for council '{_activeCouncil.Name}'.");
            }

            return path;
        }

        /// <summary>
        /// Check if there is an active council to run
        /// </summary>
        public bool HasActiveCouncil()
        {
            return _activeCouncil != null;
        }

        /// <summary>
        /// Get all configured councils
        /// </summary>
        public List<CouncilConfig> GetAllCouncils()
        {
            return _config?.Councils ?? new List<CouncilConfig>();
        }
    }
}
