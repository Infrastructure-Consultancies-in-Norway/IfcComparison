using IfcComparison.ViewModels;
using System.Collections.Generic;

namespace IfcComparison.Tests.TestHelpers
{
    /// <summary>
    /// Provides common test data and fixtures for IFC comparison tests
    /// </summary>
    public static class TestFixtures
    {
        /// <summary>
        /// Sample property set names commonly used in tests
        /// </summary>
        public static class PropertySetNames
        {
            public const string SosKonArmering = "SOS-KON_Armering";
            public const string SosKonFelles = "SOS-KON_Felles";
            public const string QaPSet = "QA_PSET";
        }

        /// <summary>
        /// Sample entity types commonly used in tests
        /// </summary>
        public static class EntityTypes
        {
            public const string ReinforcingBar = "IIfcReinforcingBar";
            public const string Wall = "IIfcWall";
            public const string Slab = "IIfcSlab";
            public const string Beam = "IIfcBeam";
        }

        /// <summary>
        /// Sample comparison operators
        /// </summary>
        public static class ComparisonOperators
        {
            public const string ARM07 = "ARM.07";
            public const string STATUS = "STATUS";
            public const string PHASE = "PHASE";
        }

        /// <summary>
        /// Creates a basic IfcEntity for testing Contains comparison
        /// </summary>
        public static IfcEntity CreateContainsIfcEntity(
            string entity = EntityTypes.ReinforcingBar,
            string comparisonOperator = ComparisonOperators.ARM07,
            List<string> propertySets = null)
        {
            return new IfcEntity
            {
                Entity = entity,
                ComparisonMethod = IfcComparison.Enumerations.ComparisonEnumeration.Contains.ToString(),
                ComparisonOperator = comparisonOperator,
                IfcPropertySets = propertySets ?? new List<string> 
                { 
                    PropertySetNames.SosKonArmering, 
                    PropertySetNames.SosKonFelles 
                }
            };
        }

        /// <summary>
        /// Creates a basic IfcEntity for testing Identifier comparison
        /// </summary>
        public static IfcEntity CreateIdentifierIfcEntity(
            string entity = EntityTypes.ReinforcingBar,
            List<string> propertySets = null)
        {
            return new IfcEntity
            {
                Entity = entity,
                ComparisonMethod = IfcComparison.Enumerations.ComparisonEnumeration.Identifier.ToString(),
                ComparisonOperator = string.Empty,
                IfcPropertySets = propertySets ?? new List<string> 
                { 
                    PropertySetNames.SosKonArmering, 
                    PropertySetNames.SosKonFelles 
                }
            };
        }

        /// <summary>
        /// Creates a list of sample property values
        /// </summary>
        public static Dictionary<string, string> CreateSampleProperties()
        {
            return new Dictionary<string, string>
            {
                { "ARM.07", "TestValue123" },
                { "STATUS", "Active" },
                { "PHASE", "Construction" },
                { "Description", "Sample description" }
            };
        }

        /// <summary>
        /// Creates a minimal property set for testing
        /// </summary>
        public static Dictionary<string, string> CreateMinimalProperties()
        {
            return new Dictionary<string, string>
            {
                { "ARM.07", "MinimalValue" }
            };
        }

        /// <summary>
        /// Sample GlobalIds for testing
        /// </summary>
        public static class GlobalIds
        {
            public const string Object1 = "3B5I1H$lHDgfC9JqxQKR7N";
            public const string Object2 = "2O7KqrPKv1HwVxY3bZnM5J";
            public const string Object3 = "1M4IxvU7f8RwQzT2aYpL8K";
            public const string Object4 = "0L9HmtN6d7PvOwS1XWoK3I";
        }
    }
}
