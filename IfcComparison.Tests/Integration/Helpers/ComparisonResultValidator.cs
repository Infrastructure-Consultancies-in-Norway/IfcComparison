using System;
using System.Collections.Generic;
using System.Linq;
using IfcComparison.Models;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xunit;

namespace IfcComparison.Tests.Integration.Helpers
{
    /// <summary>
    /// Helper class for validating IFC comparison results in integration tests
    /// </summary>
    public static class ComparisonResultValidator
    {
        /// <summary>
        /// Validates that the comparison result is not null and has expected structure
        /// </summary>
        public static void ValidateResultStructure(IfcComparerResult result)
        {
            Assert.NotNull(result);
            Assert.NotNull(result.OldObjectsNotInNew);
            Assert.NotNull(result.NewObjectsNotInOld);
        }

        /// <summary>
        /// Validates that objects were found in at least one of the models
        /// </summary>
        public static void ValidateObjectsFound(IfcComparerResult result)
        {
            ValidateResultStructure(result);
            
            var totalOldObjects = result.OldObjectsNotInNew.Sum(s => s.IfcObjects.Count);
            var totalNewObjects = result.NewObjectsNotInOld.Sum(s => s.IfcObjects.Count);
            var totalCompared = result.ComparedIfcObjects?.Count ?? 0;
            
            var totalObjects = totalOldObjects + totalNewObjects + totalCompared;
            
            Assert.True(totalObjects > 0, 
                $"Expected to find objects in at least one model. " +
                $"Old: {totalOldObjects}, New: {totalNewObjects}, Compared: {totalCompared}");
        }

        /// <summary>
        /// Validates that the QA file was written and contains the expected property set
        /// </summary>
        public static void ValidateQAFileWritten(
            IfcStore qaModel, 
            string expectedPSetName)
        {
            Assert.NotNull(qaModel);
            
            var propertySets = qaModel.Instances
                .OfType<IIfcPropertySet>()
                .Where(ps => ps.Name == expectedPSetName)
                .ToList();
            
            Assert.True(propertySets.Any(), 
                $"Expected to find property set '{expectedPSetName}' in QA file");
        }

        /// <summary>
        /// Validates that comparison found differences between models
        /// </summary>
        public static void ValidateDifferencesFound(IfcComparerResult result)
        {
            ValidateResultStructure(result);
            
            var hasOldNotInNew = result.OldObjectsNotInNew.Any(s => s.IfcObjects.Any());
            var hasNewNotInOld = result.NewObjectsNotInOld.Any(s => s.IfcObjects.Any());
            
            Assert.True(hasOldNotInNew || hasNewNotInOld, 
                "Expected to find differences between old and new models");
        }

        /// <summary>
        /// Validates that specific entity types were found
        /// </summary>
        public static void ValidateEntityTypeFound(
            IfcComparerResult result,
            string expectedEntityType)
        {
            ValidateResultStructure(result);
            
            // Check in old objects
            var foundInOld = result.OldObjectsNotInNew
                .SelectMany(s => s.IfcObjects.Values)
                .Any(obj => obj.GetType().GetInterfaces()
                    .Any(i => i.Name == expectedEntityType));
            
            // Check in new objects
            var foundInNew = result.NewObjectsNotInOld
                .SelectMany(s => s.IfcObjects.Values)
                .Any(obj => obj.GetType().GetInterfaces()
                    .Any(i => i.Name == expectedEntityType));
            
            // Check in compared objects
            var foundInCompared = result.ComparedIfcObjects?.Keys
                .Any(obj => obj.GetType().GetInterfaces()
                    .Any(i => i.Name == expectedEntityType)) ?? false;
            
            Assert.True(foundInOld || foundInNew || foundInCompared,
                $"Expected to find entity type '{expectedEntityType}' in comparison results");
        }

        /// <summary>
        /// Validates that property sets were found for objects
        /// </summary>
        public static void ValidatePropertySetsFound(
            IfcComparerResult result,
            List<string> expectedPropertySetNames)
        {
            ValidateResultStructure(result);
            
            var allPropertySets = result.OldObjectsNotInNew
                .Concat(result.NewObjectsNotInOld)
                .Select(s => s.PropertySet?.Name?.ToString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();
            
            foreach (var expectedPSet in expectedPropertySetNames)
            {
                Assert.Contains(allPropertySets, name => name == expectedPSet);
            }
        }

        /// <summary>
        /// Validates comparison statistics
        /// </summary>
        public static void LogComparisonStatistics(IfcComparerResult result)
        {
            if (result == null) return;
            
            var oldCount = result.OldObjectsNotInNew?.Sum(s => s.IfcObjects.Count) ?? 0;
            var newCount = result.NewObjectsNotInOld?.Sum(s => s.IfcObjects.Count) ?? 0;
            var comparedCount = result.ComparedIfcObjects?.Count ?? 0;
            
            Console.WriteLine($"Comparison Statistics:");
            Console.WriteLine($"  Objects only in Old: {oldCount}");
            Console.WriteLine($"  Objects only in New: {newCount}");
            Console.WriteLine($"  Objects compared: {comparedCount}");
            Console.WriteLine($"  Total objects: {oldCount + newCount + comparedCount}");
        }
    }
}
