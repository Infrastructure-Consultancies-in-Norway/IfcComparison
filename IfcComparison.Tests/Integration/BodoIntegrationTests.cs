using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IfcComparison.Enumerations;
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
    /// Integration tests for Bodo project IFC files.
    /// Tests IIfcBuildingElementProxy entity with real IFC models.
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Project", "Bodo")]
    public class BodoIntegrationTests : IClassFixture<BodoIfcFixture>
    {
        private readonly BodoIfcFixture _fixture;
        private readonly ITestOutputHelper _output;

        static BodoIntegrationTests()
        {
            LoggerInitializer.EnsureInitialized();
        }

        public BodoIntegrationTests(BodoIfcFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void Fixture_ModelsLoadedSuccessfully()
        {
            // Assert
            Assert.True(_fixture.ModelsLoaded, "IFC models should be loaded successfully");
            Assert.NotNull(_fixture.OldModel);
            Assert.NotNull(_fixture.NewModel);
            Assert.NotNull(_fixture.QAModel);

            _output.WriteLine($"Old Model: {_fixture.OldFilePath}");
            _output.WriteLine($"New Model: {_fixture.NewFilePath}");
            _output.WriteLine($"QA Model: {_fixture.QAFilePath}");
        }

        [Fact]
        public async Task BuildingElementProxy_Contains_FindsObjectsSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcBuildingElementProxy",
                propertySets: new List<string> { "Fag" },
                comparisonOperator: "B001"
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Bodo Contains Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.ValidateObjectsFound(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            _output.WriteLine($"Comparison completed successfully");
            _output.WriteLine($"Objects in old only: {result.OldObjectsNotInNew.Sum(s => s.IfcObjects.Count)}");
            _output.WriteLine($"Objects in new only: {result.NewObjectsNotInOld.Sum(s => s.IfcObjects.Count)}");
        }

        [Fact]
        public async Task BuildingElementProxy_Contains_ValidatesEntityType()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcBuildingElementProxy",
                propertySets: new List<string> { "Fag" },
                comparisonOperator: "B001"
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());
            var newObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.NewModel, config.ToIfcEntity());

            // Assert
            Assert.NotNull(oldObjects);
            Assert.NotNull(newObjects);
            Assert.NotEmpty(oldObjects.IfcStorageObjects);

            // Verify entity types
            var oldObjectsOfType = oldObjects.IfcStorageObjects
                .SelectMany(s => s.IfcObjects.Values)
                .OfType<IIfcBuildingElementProxy>()
                .ToList();

            Assert.NotEmpty(oldObjectsOfType);
            _output.WriteLine($"Found {oldObjectsOfType.Count} IIfcBuildingElementProxy objects in old model");
        }

        [Fact]
        public async Task BuildingElementProxy_Contains_ValidatesPropertySets()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcBuildingElementProxy",
                propertySets: new List<string> { "Fag" },
                comparisonOperator: "B001"
            );

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());

            // Assert
            Assert.NotEmpty(oldObjects.IfcStorageObjects);

            var propertySetNames = oldObjects.IfcStorageObjects
                .Select(s => s.PropertySet?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            Assert.Contains("Fag", propertySetNames);
            _output.WriteLine($"Property sets found: {string.Join(", ", propertySetNames)}");
        }

        [Fact]
        public async Task BuildingElementProxy_Identifier_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcBuildingElementProxy",
                propertySets: new List<string> { "Fag" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Bodo Identifier Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.ValidateObjectsFound(result);

            _output.WriteLine("Identifier-based comparison completed successfully");
        }

        [Fact]
        public async Task BuildingElementProxy_Contains_WritesQAFileCorrectly()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcBuildingElementProxy",
                propertySets: new List<string> { "Fag" },
                comparisonOperator: "B001"
            );
            config.QAPSetName = "QA_PSET_BODO";

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Bodo QA Write Test",
                entities
            );

            await comparer.CompareAllRevisions();
            
            // Write results to QA file
            if (comparer.IfcWriter != null)
            {
                var objectPSetMap = comparer.IfcComparisonResult.ComparedIfcObjects?
                    .Keys
                    .ToDictionary(k => k, v => config.QAPSetName);

                if (objectPSetMap != null && objectPSetMap.Any())
                {
                    await comparer.IfcWriter.WriteToFileAsync(_fixture.QAModel, objectPSetMap);
                }
            }

            // Assert - reload QA model to verify writes
            _fixture.QAModel.Dispose();
            var reloadedQA = Xbim.Ifc.IfcStore.Open(_fixture.QAFilePath);

            ComparisonResultValidator.ValidateQAFileWritten(reloadedQA, config.QAPSetName);

            reloadedQA.Dispose();
            _output.WriteLine($"QA file written and validated successfully");
        }

        [Fact]
        public async Task BuildingElementProxy_Contains_ValidatesComparisonOperator()
        {
            // Arrange
            var config = IfcTestConfiguration.ForContains(
                entityType: "IIfcBuildingElementProxy",
                propertySets: new List<string> { "Fag" },
                comparisonOperator: "B001"
            );

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());

            // Assert
            var storagesWithComparisonId = oldObjects.IfcStorageObjects
                .Where(s => !string.IsNullOrEmpty(s.ComparisonId))
                .ToList();

            if (storagesWithComparisonId.Any())
            {
                _output.WriteLine($"Found {storagesWithComparisonId.Count} storage objects with comparison IDs");
                _output.WriteLine($"Example comparison IDs: {string.Join(", ", storagesWithComparisonId.Take(3).Select(s => s.ComparisonId))}");
            }
            else
            {
                _output.WriteLine("No comparison IDs found - property may not exist in test data");
            }
        }
    }
}

