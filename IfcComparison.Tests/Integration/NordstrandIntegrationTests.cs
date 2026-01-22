using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IfcComparison.Logging;
using IfcComparison.Models;
using IfcComparison.Tests.Integration.Fixtures;
using IfcComparison.Tests.Integration.Helpers;
using IfcComparison.ViewModels;
using Xbim.Ifc4.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace IfcComparison.Tests.Integration
{
    /// <summary>
    /// Integration tests for Nordstrand project IFC files.
    /// Tests IIfcReinforcingBar entity with real IFC models.
    /// Refactored from original IfcComparerObjects.Tests.cs to use fixtures.
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Project", "Nordstrand")]
    public class NordstrandIntegrationTests : IClassFixture<NordstrandIfcFixture>
    {
        private readonly NordstrandIfcFixture _fixture;
        private readonly ITestOutputHelper _output;

        static NordstrandIntegrationTests()
        {
            LoggerInitializer.EnsureInitialized();
        }

        public NordstrandIntegrationTests(NordstrandIfcFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void Fixture_ModelsLoadedSuccessfully()
        {
            // Assert
            Assert.True(_fixture.ModelsLoaded);
            Assert.NotNull(_fixture.OldModel);
            Assert.NotNull(_fixture.NewModel);
            Assert.NotNull(_fixture.QAModel);

            _output.WriteLine($"Old Model: {_fixture.OldFilePath}");
            _output.WriteLine($"New Model: {_fixture.NewFilePath}");
        }

        [Fact]
        public async Task ReinforcingBar_Contains_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" },
                comparisonOperator: "ARM.07"
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Nordstrand Contains Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.ValidateObjectsFound(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            _output.WriteLine("Reinforcing bar comparison with Contains method completed");
        }

        [Fact]
        public async Task ReinforcingBar_Identifier_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Nordstrand Identifier Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.ValidateObjectsFound(result);

            _output.WriteLine("Reinforcing bar comparison with Identifier method completed");
        }

        [Fact]
        public async Task ReinforcingBar_ValidatesEntityType()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" },
                comparisonOperator: "ARM.07"
            );

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());
            var newObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.NewModel, config.ToIfcEntity());

            // Assert
            Assert.NotNull(oldObjects);
            Assert.NotNull(newObjects);
            Assert.NotEmpty(oldObjects.IfcStorageObjects);

            // Verify entity types
            var oldReinforcingBars = oldObjects.IfcStorageObjects
                .SelectMany(s => s.IfcObjects.Values)
                .OfType<IIfcReinforcingBar>()
                .ToList();

            Assert.NotEmpty(oldReinforcingBars);
            _output.WriteLine($"Found {oldReinforcingBars.Count} IIfcReinforcingBar objects in old model");
        }

        [Fact]
        public async Task ReinforcingBar_ValidatesPropertySets()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" },
                comparisonOperator: "ARM.07"
            );

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());

            // Assert
            Assert.NotEmpty(oldObjects.IfcStorageObjects);

            ComparisonResultValidator.ValidatePropertySetsFound(
                new IfcComparerResult 
                { 
                    OldObjectsNotInNew = oldObjects.IfcStorageObjects,
                    NewObjectsNotInOld = new List<IfcObjectStorage>()
                },
                config.PropertySets
            );

            _output.WriteLine("Property sets validated successfully");
        }

        [Fact]
        public async Task ReinforcingBar_Contains_ValidatesComparisonResults()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" },
                comparisonOperator: "ARM.07"
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Nordstrand Result Validation Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);

            // Validate that results contain expected data
            var oldOnlyCount = result.OldObjectsNotInNew.Sum(s => s.IfcObjects.Count);
            var newOnlyCount = result.NewObjectsNotInOld.Sum(s => s.IfcObjects.Count);
            var comparedCount = result.ComparedIfcObjects?.Count ?? 0;

            _output.WriteLine($"Objects only in old: {oldOnlyCount}");
            _output.WriteLine($"Objects only in new: {newOnlyCount}");
            _output.WriteLine($"Objects compared: {comparedCount}");

            // At least some objects should be found
            Assert.True(oldOnlyCount + newOnlyCount + comparedCount > 0,
                "Expected to find at least some objects in the comparison");
        }

        [Fact]
        public async Task ReinforcingBar_MultiplePropertySets_ComparesSuccessfully()
        {
            // Arrange - test with multiple property sets
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" },
                comparisonOperator: "ARM.07"
            );

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());

            // Assert
            var uniquePropertySets = oldObjects.IfcStorageObjects
                .Select(s => s.PropertySet?.Name?.ToString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            _output.WriteLine($"Found {uniquePropertySets.Count} unique property sets");
            _output.WriteLine($"Property sets: {string.Join(", ", uniquePropertySets)}");

            // Should have found at least one of the specified property sets
            Assert.True(uniquePropertySets.Intersect(config.PropertySets).Any(),
                "Expected to find at least one of the specified property sets");
        }
    }
}

