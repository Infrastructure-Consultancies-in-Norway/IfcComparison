using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using IfcComparison.Models;
using Xbim.Ifc4.Interfaces;

namespace IfcComparison.Tests
{
    public class IfcToolsTests
    {
        #region DisplayNameToInterfaceName Tests

        [Fact]
        public void DisplayNameToInterfaceName_ValidDisplayName_ReturnsInterfaceName()
        {
            // Arrange
            var displayName = "IfcWall";

            // Act
            var result = IfcTools.DisplayNameToInterfaceName(displayName);

            // Assert
            Assert.Equal("IIfcWall", result);
        }

        [Fact]
        public void DisplayNameToInterfaceName_AlreadyInterfaceName_ReturnsSame()
        {
            // Arrange
            var displayName = "IIfcWall";

            // Act
            var result = IfcTools.DisplayNameToInterfaceName(displayName);

            // Assert
            Assert.Equal("IIfcWall", result);
        }

        [Fact]
        public void DisplayNameToInterfaceName_NullInput_ReturnsNull()
        {
            // Arrange
            string? displayName = null;

            // Act
            var result = IfcTools.DisplayNameToInterfaceName(displayName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DisplayNameToInterfaceName_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var displayName = string.Empty;

            // Act
            var result = IfcTools.DisplayNameToInterfaceName(displayName);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DisplayNameToInterfaceName_NonIfcName_ReturnsSame()
        {
            // Arrange
            var displayName = "SomeOtherClass";

            // Act
            var result = IfcTools.DisplayNameToInterfaceName(displayName);

            // Assert
            Assert.Equal("SomeOtherClass", result);
        }

        #endregion

        #region InterfaceNameToDisplayName Tests

        [Fact]
        public void InterfaceNameToDisplayName_ValidInterfaceName_ReturnsDisplayName()
        {
            // Arrange
            var interfaceName = "IIfcWall";

            // Act
            var result = IfcTools.InterfaceNameToDisplayName(interfaceName);

            // Assert
            Assert.Equal("IfcWall", result);
        }

        [Fact]
        public void InterfaceNameToDisplayName_AlreadyDisplayName_ReturnsSame()
        {
            // Arrange
            var interfaceName = "IfcWall";

            // Act
            var result = IfcTools.InterfaceNameToDisplayName(interfaceName);

            // Assert
            Assert.Equal("IfcWall", result);
        }

        [Fact]
        public void InterfaceNameToDisplayName_NullInput_ReturnsNull()
        {
            // Arrange
            string? interfaceName = null;

            // Act
            var result = IfcTools.InterfaceNameToDisplayName(interfaceName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void InterfaceNameToDisplayName_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var interfaceName = string.Empty;

            // Act
            var result = IfcTools.InterfaceNameToDisplayName(interfaceName);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region GetInterfaceType Tests

        [Fact]
        public void GetInterfaceType_ValidInterfaceName_ReturnsType()
        {
            // Arrange
            var interfaceName = "IIfcWall";

            // Act
            var result = IfcTools.GetInterfaceType(interfaceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IIfcWall", result.Name);
            Assert.True(result.IsInterface);
        }

        [Fact]
        public void GetInterfaceType_ValidDisplayName_ReturnsType()
        {
            // Arrange
            var displayName = "IfcWall";

            // Act
            var result = IfcTools.GetInterfaceType(displayName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IIfcWall", result.Name);
        }

        [Fact]
        public void GetInterfaceType_InvalidName_ReturnsNull()
        {
            // Arrange
            var invalidName = "InvalidTypeName";

            // Act
            var result = IfcTools.GetInterfaceType(invalidName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInterfaceType_NullInput_ReturnsNull()
        {
            // Arrange
            string? name = null;

            // Act
            var result = IfcTools.GetInterfaceType(name);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInterfaceType_EmptyString_ReturnsNull()
        {
            // Arrange
            var name = string.Empty;

            // Act
            var result = IfcTools.GetInterfaceType(name);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("IIfcBeam")]
        [InlineData("IIfcSlab")]
        [InlineData("IIfcColumn")]
        [InlineData("IIfcDoor")]
        [InlineData("IIfcWindow")]
        public void GetInterfaceType_CommonIfcTypes_ReturnsValidTypes(string typeName)
        {
            // Act
            var result = IfcTools.GetInterfaceType(typeName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(typeName, result.Name);
            Assert.True(result.FullName.Contains("Ifc4"));
        }

        [Theory]
        [InlineData("IfcBeam")]
        [InlineData("IfcSlab")]
        [InlineData("IfcColumn")]
        public void GetInterfaceType_DisplayNames_ReturnsValidTypes(string displayName)
        {
            // Act
            var result = IfcTools.GetInterfaceType(displayName);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsInterface);
            Assert.True(result.FullName.Contains("Ifc4"));
        }

        #endregion

        #region IfcEntities Property Tests

        [Fact]
        public void IfcEntities_NotNull()
        {
            // Act & Assert
            Assert.NotNull(IfcTools.IfcEntities);
        }

        [Fact]
        public void IfcEntities_ContainsMultipleTypes()
        {
            // Act
            var count = IfcTools.IfcEntities.Count;

            // Assert
            Assert.True(count > 0, "IfcEntities should contain at least one type");
        }

        [Fact]
        public void IfcEntities_ContainsCommonIfcInterfaces()
        {
            // Arrange
            var expectedTypes = new[] { "IIfcWall", "IIfcSlab", "IIfcBeam", "IIfcDoor", "IIfcWindow" };

            // Act
            var entityNames = IfcTools.IfcEntities.Select(t => t.Name).ToList();

            // Assert
            foreach (var expectedType in expectedTypes)
            {
                Assert.Contains(expectedType, entityNames);
            }
        }

        [Fact]
        public void IfcEntities_MostTypesAreInterfaces()
        {
            // Act
            var interfaceCount = IfcTools.IfcEntities.Count(t => t.IsInterface);
            var totalCount = IfcTools.IfcEntities.Count;

            // Assert - Most types should be interfaces (at least 75%)
            Assert.True(interfaceCount > totalCount * 0.75, 
                $"Expected most types to be interfaces, but only {interfaceCount} out of {totalCount} are interfaces");
        }

        #endregion
    }
}
