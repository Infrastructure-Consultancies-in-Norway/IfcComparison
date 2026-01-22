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
    /// Integration tests for Grettefoss bridge project IFC files.
    /// Tests common bridge structural elements.
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Project", "Grettefoss")]
    public class GrettefossIntegrationTests : IClassFixture<GrettefossIfcFixture>
    {
        private readonly GrettefossIfcFixture _fixture;
        private readonly ITestOutputHelper _output;

        static GrettefossIntegrationTests()
        {
            LoggerInitializer.EnsureInitialized();
        }

        public GrettefossIntegrationTests(GrettefossIfcFixture fixture, ITestOutputHelper output)
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

            _output.WriteLine($"Old Model: {_fixture.OldFilePath}");
            _output.WriteLine($"New Model: {_fixture.NewFilePath}");
        }

        [Fact]
        public async Task Beam_Identifier_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcBeam",
                propertySets: new List<string> { "Pset_BeamCommon" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Grettefoss Beam Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            _output.WriteLine("Beam comparison completed");
        }

        [Fact]
        public async Task Slab_Identifier_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcSlab",
                propertySets: new List<string> { "Pset_SlabCommon" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Grettefoss Slab Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            _output.WriteLine("Slab comparison completed");
        }

        [Fact]
        public async Task MultipleEntityTypes_ComparesSuccessfully()
        {
            // Arrange - test multiple entity types in one comparison
            var beamConfig = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcBeam",
                propertySets: new List<string> { "Pset_BeamCommon" }
            );

            var slabConfig = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcSlab",
                propertySets: new List<string> { "Pset_SlabCommon" }
            );

            var entities = new List<IfcEntity> 
            { 
                beamConfig.ToIfcEntity(),
                slabConfig.ToIfcEntity()
            };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Grettefoss Multiple Entities Test",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.ValidateObjectsFound(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            _output.WriteLine("Multiple entity types comparison completed");
        }

        [Fact]
        public async Task Beam_CountsObjectsInBothModels()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcBeam",
                propertySets: new List<string> { "Pset_BeamCommon" }
            );

            // Act
            var oldObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());
            var newObjects = await Models.IfcComparerObjects.CreateAsync(_fixture.NewModel, config.ToIfcEntity());

            // Assert
            var oldBeamCount = oldObjects.IfcStorageObjects
                .SelectMany(s => s.IfcObjects.Values)
                .OfType<IIfcBeam>()
                .Count();

            var newBeamCount = newObjects.IfcStorageObjects
                .SelectMany(s => s.IfcObjects.Values)
                .OfType<IIfcBeam>()
                .Count();

            _output.WriteLine($"Old model beams: {oldBeamCount}");
            _output.WriteLine($"New model beams: {newBeamCount}");

            // At least one model should have beams
            Assert.True(oldBeamCount > 0 || newBeamCount > 0,
                "Expected to find beams in at least one model");
        }
    }
}

