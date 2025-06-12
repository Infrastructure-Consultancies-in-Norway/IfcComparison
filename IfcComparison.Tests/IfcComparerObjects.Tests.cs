
using IfcComparison.Enumerations;
using IfcComparison.ViewModels;
using Xbim.Ifc;
using Xunit;

namespace IfcComparison.Tests
{
    public class IfcComparerObjects
    {

        private static IfcStore IfcModel { get; set; }

        private static List<string> _pSetToCheck = new List<string> { "SOS-KON_Armering", "SOS_Mengde" };

        private static IfcEntity _entity = new IfcEntity() { ComparisonMethod = ComparisonEnumeration.Contains.ToString(), ComparisonOperator = "ARM.07", Entity = "IIfcReinforcingBar", IfcPropertySets = _pSetToCheck };

        // Initialize the IfcModel before running tests
        [Fact]
        public void Initialize_IfcModel()
        {
            //Relative path to the IFC file, adjust as necessary
            var ifcFilePath = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_03C.ifc";

            IfcModel = IfcStore.Open(ifcFilePath);
            // Ensure the model is loaded correctly
            Assert.NotNull(IfcModel);
        }

        [Fact]
        // Check if the IfcComparerObjects class can be instantiated with a valid IfcStore and IfcEntity
        public void Initialize_IfcComparerObjects()
        {
            if (IfcModel == null)
            {
                Initialize_IfcModel(); // Ensure the model is initialized before testing
            }

            var ifcComparerObjects = new Models.IfcComparerObjects(IfcModel, _entity);
            Assert.NotNull(ifcComparerObjects);
        }








    }
}
