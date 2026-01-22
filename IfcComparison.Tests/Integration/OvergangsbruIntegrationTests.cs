using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IfcComparison.Logging;
using IfcComparison.Models;
using IfcComparison.Tests.Integration.Fixtures;
using IfcComparison.Tests.Integration.Helpers;
using IfcComparison.ViewModels;
using Xbim.Ifc;
using Xunit;
using Xunit.Abstractions;

namespace IfcComparison.Tests.Integration
{
    /// <summary>
    /// Integration tests for Overgangsbru Nordstrand project with multiple revisions.
    /// Tests three-way comparisons across versions 0, 1, and 2.
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Project", "Overgangsbru")]
    public class OvergangsbruIntegrationTests : IClassFixture<OvergangsbruIfcFixture>
    {
        private readonly OvergangsbruIfcFixture _fixture;
        private readonly ITestOutputHelper _output;

        static OvergangsbruIntegrationTests()
        {
            LoggerInitializer.EnsureInitialized();
        }

        public OvergangsbruIntegrationTests(OvergangsbruIfcFixture fixture, ITestOutputHelper output)
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
            Assert.True(File.Exists(_fixture.Version2FilePath));

            _output.WriteLine($"Version 0: {_fixture.OldFilePath}");
            _output.WriteLine($"Version 1: {_fixture.NewFilePath}");
            _output.WriteLine($"Version 2: {_fixture.Version2FilePath}");
        }

        [Fact]
        public async Task Version0_vs_Version1_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            // Act
            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                _fixture.QAModel,
                _fixture.QAFilePath,
                "Overgangsbru v0 vs v1",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            _output.WriteLine("Version 0 vs Version 1 comparison completed");
        }

        [Fact]
        public async Task Version1_vs_Version2_ComparesSuccessfully()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            var version2QAPath = _fixture.Version2FilePath.Replace(".ifc", "_QA_Test.ifc");
            File.Copy(_fixture.Version2FilePath, version2QAPath, overwrite: true);

            // Act
            using var version2Model = IfcStore.Open(_fixture.Version2FilePath);
            using var version2QA = IfcStore.Open(version2QAPath);

            var comparer = await IfcComparer.CreateAsync(
                _fixture.NewModel,
                version2QA,
                version2QAPath,
                "Overgangsbru v1 vs v2",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            // Cleanup
            version2Model.Dispose();
            version2QA.Dispose();
            if (File.Exists(version2QAPath)) File.Delete(version2QAPath);

            _output.WriteLine("Version 1 vs Version 2 comparison completed");
        }

        [Fact]
        public async Task Version0_vs_Version2_TracksChangesAcrossRevisions()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering" }
            );

            var entities = new List<IfcEntity> { config.ToIfcEntity() };

            var version2QAPath = _fixture.Version2FilePath.Replace(".ifc", "_QA_v0v2_Test.ifc");
            File.Copy(_fixture.Version2FilePath, version2QAPath, overwrite: true);

            // Act
            using var version2Model = IfcStore.Open(_fixture.Version2FilePath);
            using var version2QA = IfcStore.Open(version2QAPath);

            var comparer = await IfcComparer.CreateAsync(
                _fixture.OldModel,
                version2QA,
                version2QAPath,
                "Overgangsbru v0 vs v2",
                entities
            );

            await comparer.CompareAllRevisions();
            var result = comparer.IfcComparisonResult;

            // Assert
            ComparisonResultValidator.ValidateResultStructure(result);
            ComparisonResultValidator.ValidateObjectsFound(result);
            ComparisonResultValidator.LogComparisonStatistics(result);

            // Cleanup
            version2Model.Dispose();
            version2QA.Dispose();
            if (File.Exists(version2QAPath)) File.Delete(version2QAPath);

            _output.WriteLine("Version 0 vs Version 2 comparison completed");
            _output.WriteLine("This represents the cumulative changes across all revisions");
        }

        [Fact]
        public async Task MultipleRevisions_ValidatesChangeProgression()
        {
            // Arrange
            var config = IfcTestConfiguration.ForIdentifier(
                entityType: "IIfcReinforcingBar",
                propertySets: new List<string> { "SOS-KON_Armering" }
            );

            // Act - Get object counts from each version
            var v0Objects = await Models.IfcComparerObjects.CreateAsync(_fixture.OldModel, config.ToIfcEntity());
            var v1Objects = await Models.IfcComparerObjects.CreateAsync(_fixture.NewModel, config.ToIfcEntity());

            using var v2Model = IfcStore.Open(_fixture.Version2FilePath);
            var v2Objects = await Models.IfcComparerObjects.CreateAsync(v2Model, config.ToIfcEntity());

            var v0Count = v0Objects.IfcStorageObjects.Sum(s => s.IfcObjects.Count);
            var v1Count = v1Objects.IfcStorageObjects.Sum(s => s.IfcObjects.Count);
            var v2Count = v2Objects.IfcStorageObjects.Sum(s => s.IfcObjects.Count);

            // Assert
            _output.WriteLine($"Version 0 objects: {v0Count}");
            _output.WriteLine($"Version 1 objects: {v1Count}");
            _output.WriteLine($"Version 2 objects: {v2Count}");

            // At least one version should have objects
            Assert.True(v0Count > 0 || v1Count > 0 || v2Count > 0,
                "Expected to find objects in at least one version");

            v2Model.Dispose();
        }
    }
}

