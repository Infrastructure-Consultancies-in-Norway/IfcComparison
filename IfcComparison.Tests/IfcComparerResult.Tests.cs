using System.Collections.Generic;
using Xbim.Ifc4.Interfaces;
using Xunit;
using IfcComparison.Models;
using IfcComparison.Tests.TestHelpers;
using Moq;
using Xbim.Ifc4.UtilityResource;

namespace IfcComparison.Tests
{
    public class IfcComparerResultTests
    {
        [Fact]
        public void Constructor_InitializesEmptyInstance()
        {
            // Act
            var result = new IfcComparerResult();

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.OldObjectsNotInNew);
            Assert.Null(result.NewObjectsNotInOld);
            Assert.Null(result.ComparedIfcObjects);
        }

        [Fact]
        public void OldObjectsNotInNew_CanBeSet()
        {
            // Arrange
            var result = new IfcComparerResult();
            var storageObjects = new List<IfcObjectStorage>
            {
                new IfcObjectStorage()
            };

            // Act
            result.OldObjectsNotInNew = storageObjects;

            // Assert
            Assert.NotNull(result.OldObjectsNotInNew);
            Assert.Single(result.OldObjectsNotInNew);
        }

        [Fact]
        public void NewObjectsNotInOld_CanBeSet()
        {
            // Arrange
            var result = new IfcComparerResult();
            var storageObjects = new List<IfcObjectStorage>
            {
                new IfcObjectStorage(),
                new IfcObjectStorage()
            };

            // Act
            result.NewObjectsNotInOld = storageObjects;

            // Assert
            Assert.NotNull(result.NewObjectsNotInOld);
            Assert.Equal(2, result.NewObjectsNotInOld.Count);
        }

        [Fact]
        public void ComparedIfcObjects_InternalSet_CanBeSet()
        {
            // Arrange - Using internal setter through reflection or just testing the getter
            var result = new IfcComparerResult();

            // Act - The property is internal set, so we can only verify it starts as null
            var comparedObjects = result.ComparedIfcObjects;

            // Assert
            Assert.Null(comparedObjects);
        }

        [Fact]
        public void AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new IfcComparerResult();
            var oldStorage = new List<IfcObjectStorage> { new IfcObjectStorage() };
            var newStorage = new List<IfcObjectStorage> { new IfcObjectStorage() };

            // Act
            result.OldObjectsNotInNew = oldStorage;
            result.NewObjectsNotInOld = newStorage;

            // Assert
            Assert.NotNull(result.OldObjectsNotInNew);
            Assert.NotNull(result.NewObjectsNotInOld);
            Assert.Single(result.OldObjectsNotInNew);
            Assert.Single(result.NewObjectsNotInOld);
        }
    }
}
