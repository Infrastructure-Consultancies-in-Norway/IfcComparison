using System.Collections.Generic;
using IfcComparison.Enumerations;

namespace IfcComparison.Tests.Integration.Helpers
{
    /// <summary>
    /// Test configuration model for defining IFC comparison test scenarios
    /// </summary>
    public class IfcTestConfiguration
    {
        public string EntityType { get; set; }
        public List<string> PropertySets { get; set; }
        public string ComparisonOperator { get; set; }
        public ComparisonEnumeration ComparisonMethod { get; set; }
        public string QAPSetName { get; set; } = "QA_PSET";

        /// <summary>
        /// Creates a configuration for Contains comparison method
        /// </summary>
        public static IfcTestConfiguration ForContains(
            string entityType, 
            List<string> propertySets, 
            string comparisonOperator)
        {
            return new IfcTestConfiguration
            {
                EntityType = entityType,
                PropertySets = propertySets,
                ComparisonOperator = comparisonOperator,
                ComparisonMethod = ComparisonEnumeration.Contains
            };
        }

        /// <summary>
        /// Creates a configuration for Identifier comparison method
        /// </summary>
        public static IfcTestConfiguration ForIdentifier(
            string entityType, 
            List<string> propertySets)
        {
            return new IfcTestConfiguration
            {
                EntityType = entityType,
                PropertySets = propertySets,
                ComparisonOperator = string.Empty,
                ComparisonMethod = ComparisonEnumeration.Identifier
            };
        }

        /// <summary>
        /// Converts configuration to IfcEntity for use with comparison logic
        /// </summary>
        public ViewModels.IfcEntity ToIfcEntity()
        {
            return new ViewModels.IfcEntity
            {
                Entity = EntityType,
                IfcPropertySets = PropertySets,
                ComparisonOperator = ComparisonOperator,
                ComparisonMethod = ComparisonMethod.ToString(),
                PSetName = QAPSetName
            };
        }
    }
}
