using IfcComparison.Logging;
using IfcComparison.Models;
using IfcComparison.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xbim.Common.Step21;
using Xbim.Ifc;

namespace IfcComparison.CLI
{
    public class CliRunner
    {
        private readonly ILogger _logger;

        public CliRunner()
        {
            LoggerInitializer.EnsureInitialized();
            _logger = LoggingService.CreateLogger<CliRunner>();
        }

        public async Task<int> RunAsync(string settingsFilePath)
        {
            try
            {
                Console.WriteLine("IfcComparison CLI - Starting comparison...");
                _logger.LogInformation("CLI mode started with settings file: {SettingsFile}", settingsFilePath);

                // Validate and load settings
                var validationResult = ValidateSettingsFile(settingsFilePath);
                if (!validationResult.IsValid)
                {
                    Console.Error.WriteLine("ERROR: Settings file validation failed:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.Error.WriteLine($"  - {error}");
                    }
                    return 1;
                }

                var settings = validationResult.Settings;

                // Display loaded configuration
                Console.WriteLine("\nConfiguration loaded:");
                Console.WriteLine($"  Old IFC: {settings.FilePathOldIFC}");
                Console.WriteLine($"  New IFC: {settings.FilePathNewIFC}");
                Console.WriteLine($"  Output QA IFC: {settings.FilePathIFCToQA}");
                Console.WriteLine($"  Entity count: {settings.DataGridContentIFCEntities?.Count ?? 0}");

                // Load IFC models
                Console.WriteLine("\nLoading IFC models...");
                IfcStore oldModel = null;
                IfcStore newModel = null;

                try
                {
                    oldModel = await OpenIFCModelAsync(settings.FilePathOldIFC, "Old IFC");
                    newModel = await OpenIFCModelAsync(settings.FilePathNewIFC, "New IFC");

                    // Validate entity configuration
                    var entityValidation = ValidateEntityConfiguration(settings.DataGridContentIFCEntities);
                    if (!entityValidation.IsValid)
                    {
                        Console.Error.WriteLine("\nERROR: Entity configuration validation failed:");
                        foreach (var error in entityValidation.Errors)
                        {
                            Console.Error.WriteLine($"  - {error}");
                        }
                        return 1;
                    }

                    // Ensure output directory exists
                    var outputDir = Path.GetDirectoryName(settings.FilePathIFCToQA);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                        Console.WriteLine($"Created output directory: {outputDir}");
                    }

                    // Run comparison
                    Console.WriteLine("\nStarting IFC comparison...");
                    var comparer = await IfcComparer.CreateAsync(
                        oldModel,
                        newModel,
                        settings.FilePathIFCToQA,
                        "CLI Transaction",
                        settings.DataGridContentIFCEntities.ToList());

                    await comparer.CompareAllRevisions();

                    Console.WriteLine("\n✓ IFC Comparison completed successfully!");
                    Console.WriteLine($"Output written to: {settings.FilePathIFCToQA}");
                    _logger.LogInformation("CLI comparison completed successfully");

                    return 0;
                }
                finally
                {
                    // Clean up models
                    oldModel?.Close();
                    newModel?.Close();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nERROR: {ex.Message}");
                _logger.LogError(ex, "CLI comparison failed");
                return 1;
            }
        }

        private ValidationResult ValidateSettingsFile(string filePath)
        {
            var result = new ValidationResult();

            // Check file exists
            if (!File.Exists(filePath))
            {
                result.Errors.Add($"Settings file not found: {filePath}");
                return result;
            }

            try
            {
                // Read and parse JSON
                var jsonContent = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (settings == null)
                {
                    result.Errors.Add("Failed to deserialize settings file. The JSON structure may be invalid.");
                    return result;
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(settings.FilePathOldIFC))
                {
                    result.Errors.Add("FilePathOldIFC is required");
                }
                else if (!File.Exists(settings.FilePathOldIFC))
                {
                    result.Errors.Add($"Old IFC file not found: {settings.FilePathOldIFC}");
                }

                if (string.IsNullOrWhiteSpace(settings.FilePathNewIFC))
                {
                    result.Errors.Add("FilePathNewIFC is required");
                }
                else if (!File.Exists(settings.FilePathNewIFC))
                {
                    result.Errors.Add($"New IFC file not found: {settings.FilePathNewIFC}");
                }

                if (string.IsNullOrWhiteSpace(settings.FilePathIFCToQA))
                {
                    result.Errors.Add("FilePathIFCToQA (output path) is required");
                }

                if (settings.DataGridContentIFCEntities == null || settings.DataGridContentIFCEntities.Count == 0)
                {
                    result.Errors.Add("DataGridContentIFCEntities must contain at least one entity configuration");
                }

                result.Settings = settings;
                result.IsValid = result.Errors.Count == 0;
                return result;
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"JSON parsing error: {ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unexpected error reading settings file: {ex.Message}");
                return result;
            }
        }

        private ValidationResult ValidateEntityConfiguration(System.Collections.ObjectModel.ObservableCollection<IfcEntity> entities)
        {
            var result = new ValidationResult { IsValid = true };

            if (entities == null || entities.Count == 0)
            {
                result.Errors.Add("No entity configurations provided");
                result.IsValid = false;
                return result;
            }

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var prefix = $"Entity {i + 1}";

                if (string.IsNullOrWhiteSpace(entity.Entity))
                {
                    result.Errors.Add($"{prefix}: Entity type is required");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(entity.PSetName))
                {
                    result.Errors.Add($"{prefix}: PSetName is required");
                    result.IsValid = false;
                }

                if (entity.IfcPropertySets == null || entity.IfcPropertySets.Count == 0)
                {
                    result.Errors.Add($"{prefix}: At least one IfcPropertySet is required");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(entity.ComparisonOperator))
                {
                    result.Errors.Add($"{prefix}: ComparisonOperator is required");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(entity.ComparisonMethod))
                {
                    result.Errors.Add($"{prefix}: ComparisonMethod is required");
                    result.IsValid = false;
                }
                else if (!IsValidComparisonMethod(entity.ComparisonMethod))
                {
                    result.Errors.Add($"{prefix}: Invalid ComparisonMethod '{entity.ComparisonMethod}'. Valid values are: Equals, Contains, StartsWith, EndsWith");
                    result.IsValid = false;
                }
            }

            return result;
        }

        private bool IsValidComparisonMethod(string method)
        {
            var validMethods = new[] { "Equals", "Contains", "StartsWith", "EndsWith" };
            return validMethods.Contains(method, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IfcStore> OpenIFCModelAsync(string fileName, string description)
        {
            Console.WriteLine($"  Loading {description}: {Path.GetFileName(fileName)}...");
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();

            var model = await Task.Run(() =>
            {
                var editor = new XbimEditorCredentials
                {
                    ApplicationDevelopersName = "COWI",
                    ApplicationFullName = "IfcComparison CLI",
                    ApplicationIdentifier = "IfcComparison",
                    ApplicationVersion = "2.1.0"
                };

                return IfcStore.Open(fileName, editor, accessMode: Xbim.IO.XbimDBAccess.Read);
            });

            Console.WriteLine($"  ✓ {description} loaded successfully");
            _logger.LogInformation("{Description} model loaded: {FileName}", description, fileName);

            return model;
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; } = new List<string>();
            public UserSettings Settings { get; set; }
        }
    }
}
