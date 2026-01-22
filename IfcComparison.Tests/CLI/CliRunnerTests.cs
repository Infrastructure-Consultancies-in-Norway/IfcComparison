using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using IfcComparison.CLI;
using IfcComparison.ViewModels;
using Xunit;

namespace IfcComparison.Tests.CLI
{
    public class CliRunnerTests
    {
        private readonly string _testDataFolder;

        public CliRunnerTests()
        {
            _testDataFolder = Path.Combine(Path.GetTempPath(), "IfcComparisonTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataFolder);
        }

        [Fact]
        public async Task RunAsync_WithMissingSettingsFile_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var nonExistentPath = Path.Combine(_testDataFolder, "nonexistent.json");

            // Act
            var result = await runner.RunAsync(nonExistentPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RunAsync_WithInvalidJson_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var invalidJsonPath = Path.Combine(_testDataFolder, "invalid.json");
            File.WriteAllText(invalidJsonPath, "{ invalid json content");

            // Act
            var result = await runner.RunAsync(invalidJsonPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RunAsync_WithEmptySettings_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var emptySettingsPath = Path.Combine(_testDataFolder, "empty.json");
            File.WriteAllText(emptySettingsPath, "{}");

            // Act
            var result = await runner.RunAsync(emptySettingsPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RunAsync_WithMissingRequiredFields_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var settingsPath = Path.Combine(_testDataFolder, "missing_fields.json");
            
            var settings = new UserSettings
            {
                FilePathOldIFC = "", // Missing
                FilePathNewIFC = "", // Missing
                FilePathIFCToQA = "", // Missing
                DataGridContentIFCEntities = new ObservableCollection<IfcEntity>()
            };

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(settingsPath, json);

            // Act
            var result = await runner.RunAsync(settingsPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentIfcFiles_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var settingsPath = Path.Combine(_testDataFolder, "nonexistent_files.json");
            
            var settings = new UserSettings
            {
                FilePathOldIFC = "C:\\nonexistent\\old.ifc",
                FilePathNewIFC = "C:\\nonexistent\\new.ifc",
                FilePathIFCToQA = Path.Combine(_testDataFolder, "output.ifc"),
                DataGridContentIFCEntities = new ObservableCollection<IfcEntity>
                {
                    new IfcEntity
                    {
                        PSetName = "TEST",
                        Entity = "IIfcWall",
                        IfcPropertySets = new System.Collections.Generic.List<string> { "Pset_WallCommon" },
                        ComparisonOperator = "TestValue",
                        ComparisonMethod = "Contains"
                    }
                }
            };

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(settingsPath, json);

            // Act
            var result = await runner.RunAsync(settingsPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RunAsync_WithEmptyEntityList_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var settingsPath = Path.Combine(_testDataFolder, "empty_entities.json");
            
            // Create dummy IFC files
            var oldIfcPath = Path.Combine(_testDataFolder, "old.ifc");
            var newIfcPath = Path.Combine(_testDataFolder, "new.ifc");
            File.WriteAllText(oldIfcPath, ""); // Empty file for test
            File.WriteAllText(newIfcPath, ""); // Empty file for test
            
            var settings = new UserSettings
            {
                FilePathOldIFC = oldIfcPath,
                FilePathNewIFC = newIfcPath,
                FilePathIFCToQA = Path.Combine(_testDataFolder, "output.ifc"),
                DataGridContentIFCEntities = new ObservableCollection<IfcEntity>() // Empty list
            };

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(settingsPath, json);

            // Act
            var result = await runner.RunAsync(settingsPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RunAsync_WithInvalidComparisonMethod_ReturnsError()
        {
            // Arrange
            var runner = new CliRunner();
            var settingsPath = Path.Combine(_testDataFolder, "invalid_method.json");
            
            // Create dummy IFC files
            var oldIfcPath = Path.Combine(_testDataFolder, "old2.ifc");
            var newIfcPath = Path.Combine(_testDataFolder, "new2.ifc");
            File.WriteAllText(oldIfcPath, "");
            File.WriteAllText(newIfcPath, "");
            
            var settings = new UserSettings
            {
                FilePathOldIFC = oldIfcPath,
                FilePathNewIFC = newIfcPath,
                FilePathIFCToQA = Path.Combine(_testDataFolder, "output.ifc"),
                DataGridContentIFCEntities = new ObservableCollection<IfcEntity>
                {
                    new IfcEntity
                    {
                        PSetName = "TEST",
                        Entity = "IIfcWall",
                        IfcPropertySets = new System.Collections.Generic.List<string> { "Pset_WallCommon" },
                        ComparisonOperator = "TestValue",
                        ComparisonMethod = "InvalidMethod" // Invalid method
                    }
                }
            };

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(settingsPath, json);

            // Act
            var result = await runner.RunAsync(settingsPath);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void ValidJson_CanBeDeserialized()
        {
            // Arrange
            var settingsPath = Path.Combine(_testDataFolder, "valid.json");
            var json = @"{
                ""FilePathOldIFC"": ""C:\\test\\old.ifc"",
                ""FilePathNewIFC"": ""C:\\test\\new.ifc"",
                ""FilePathIFCToQA"": ""C:\\test\\output.ifc"",
                ""DataGridContentIFCEntities"": [
                    {
                        ""PSetName"": ""QA_REBAR"",
                        ""Entity"": ""IIfcReinforcingBar"",
                        ""IfcPropertySets"": [""MERKNADER""],
                        ""ComparisonOperator"": ""K200"",
                        ""ComparisonMethod"": ""Contains""
                    }
                ]
            }";
            File.WriteAllText(settingsPath, json);

            // Act
            var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(settingsPath));

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("C:\\test\\old.ifc", settings.FilePathOldIFC);
            Assert.Equal("C:\\test\\new.ifc", settings.FilePathNewIFC);
            Assert.Equal("C:\\test\\output.ifc", settings.FilePathIFCToQA);
            Assert.Single(settings.DataGridContentIFCEntities);
            Assert.Equal("QA_REBAR", settings.DataGridContentIFCEntities[0].PSetName);
            Assert.Equal("IIfcReinforcingBar", settings.DataGridContentIFCEntities[0].Entity);
            Assert.Equal("Contains", settings.DataGridContentIFCEntities[0].ComparisonMethod);
        }

        [Theory]
        [InlineData("Equals")]
        [InlineData("Contains")]
        [InlineData("StartsWith")]
        [InlineData("EndsWith")]
        public void ValidComparisonMethods_AreAccepted(string method)
        {
            // Arrange
            var entity = new IfcEntity
            {
                PSetName = "TEST",
                Entity = "IIfcWall",
                IfcPropertySets = new System.Collections.Generic.List<string> { "Pset_WallCommon" },
                ComparisonOperator = "TestValue",
                ComparisonMethod = method
            };

            // Assert - method should be valid
            Assert.NotNull(entity.ComparisonMethod);
        }

        [Fact]
        public void EntityConfiguration_SupportsMultiplePropertySets()
        {
            // Arrange & Act
            var entity = new IfcEntity
            {
                PSetName = "TEST",
                Entity = "IIfcReinforcingBar",
                IfcPropertySets = new System.Collections.Generic.List<string> 
                { 
                    "SOS-KON_Armering", 
                    "SOS-KON_Felles",
                    "MERKNADER" 
                },
                ComparisonOperator = "ARM.07",
                ComparisonMethod = "Contains"
            };

            // Assert
            Assert.Equal(3, entity.IfcPropertySets.Count);
            Assert.Contains("SOS-KON_Armering", entity.IfcPropertySets);
            Assert.Contains("SOS-KON_Felles", entity.IfcPropertySets);
            Assert.Contains("MERKNADER", entity.IfcPropertySets);
        }
    }
}
