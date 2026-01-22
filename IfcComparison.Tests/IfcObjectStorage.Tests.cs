using System;
using System.Collections.Generic;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;
using Xunit;
using IfcComparison.Models;
using IfcComparison.Tests.TestHelpers;
using Moq;

namespace IfcComparison.Tests
{
    public class IfcObjectStorageTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            // Act
            var storage = new IfcObjectStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.NotNull(storage.IfcObjects);
            Assert.Empty(storage.IfcObjects);
            Assert.Equal(string.Empty, storage.ComparisonId);
            Assert.Null(storage.PropertySet);
        }

        [Fact]
        public void IfcObjects_CanBeSet()
        {
            // Arrange
            var storage = new IfcObjectStorage();
            var mockObject1 = IfcMockBuilder.CreateMockIfcObject(TestFixtures.GlobalIds.Object1, 1);
            var mockObject2 = IfcMockBuilder.CreateMockIfcObject(TestFixtures.GlobalIds.Object2, 2);
            
            var objects = new Dictionary<IfcGloballyUniqueId, IIfcObject>
            {
                { mockObject1.Object.GlobalId, mockObject1.Object },
                { mockObject2.Object.GlobalId, mockObject2.Object }
            };

            // Act
            storage.IfcObjects = objects;

            // Assert
            Assert.NotNull(storage.IfcObjects);
            Assert.Equal(2, storage.IfcObjects.Count);
        }

        [Fact]
        public void ComparisonId_CanBeSet()
        {
            // Arrange
            var storage = new IfcObjectStorage();
            var comparisonId = "TestValue123";

            // Act
            storage.ComparisonId = comparisonId;

            // Assert
            Assert.Equal(comparisonId, storage.ComparisonId);
        }

        [Fact]
        public void PropertySet_CanBeSet()
        {
            // Arrange
            var storage = new IfcObjectStorage();
            var mockPropertySet = IfcMockBuilder.CreateMockPropertySet(
                TestFixtures.PropertySetNames.SosKonArmering,
                TestFixtures.CreateSampleProperties());

            // Act
            storage.PropertySet = mockPropertySet.Object;

            // Assert
            Assert.NotNull(storage.PropertySet);
            Assert.Equal(TestFixtures.PropertySetNames.SosKonArmering, storage.PropertySet.Name);
        }

        [Fact]
        public void AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var storage = new IfcObjectStorage();
            var mockObject = IfcMockBuilder.CreateMockIfcObject();
            var mockPropertySet = IfcMockBuilder.CreateMockPropertySet("TestPSet");
            
            var objects = new Dictionary<IfcGloballyUniqueId, IIfcObject>
            {
                { mockObject.Object.GlobalId, mockObject.Object }
            };

            // Act
            storage.IfcObjects = objects;
            storage.ComparisonId = "CompId123";
            storage.PropertySet = mockPropertySet.Object;

            // Assert
            Assert.Single(storage.IfcObjects);
            Assert.Equal("CompId123", storage.ComparisonId);
            Assert.NotNull(storage.PropertySet);
            Assert.Equal("TestPSet", storage.PropertySet.Name);
        }

        [Fact]
        public void IfcObjects_EmptyDictionary_IsValid()
        {
            // Arrange
            var storage = new IfcObjectStorage
            {
                IfcObjects = new Dictionary<IfcGloballyUniqueId, IIfcObject>()
            };

            // Assert
            Assert.NotNull(storage.IfcObjects);
            Assert.Empty(storage.IfcObjects);
        }

        [Fact]
        public void ComparisonId_EmptyString_IsValid()
        {
            // Arrange
            var storage = new IfcObjectStorage
            {
                ComparisonId = string.Empty
            };

            // Assert
            Assert.Equal(string.Empty, storage.ComparisonId);
        }

        [Fact]
        public void IfcObjects_MultipleObjects_CanBeAdded()
        {
            // Arrange
            var storage = new IfcObjectStorage();
            var objects = new Dictionary<IfcGloballyUniqueId, IIfcObject>();

            for (int i = 1; i <= 5; i++)
            {
                var mockObject = IfcMockBuilder.CreateMockIfcObject($"Guid{i}", i);
                objects.Add(mockObject.Object.GlobalId, mockObject.Object);
            }

            // Act
            storage.IfcObjects = objects;

            // Assert
            Assert.Equal(5, storage.IfcObjects.Count);
        }
    }
}
