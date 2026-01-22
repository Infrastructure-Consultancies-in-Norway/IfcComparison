using System;
using System.IO;

namespace IfcComparison.Tests.Integration.Fixtures
{
    /// <summary>
    /// Fixture for Overgangsbru Nordstrand project IFC files.
    /// </summary>
    public class OvergangsbruIfcFixture : IfcModelFixture
    {
        private const string BaseDirectory = "../../../IfcFiles/Overgangsbru_Nordstrand";
        
        public OvergangsbruIfcFixture() : base(
            oldFilePath: Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_0.ifc"),
            newFilePath: Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_1.ifc"),
            qaFilePath: Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_1_QA_Test.ifc")
        )
        {
        }

        // Additional properties for three-way testing
        public string Version2FilePath => Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_2.ifc");
    }
}
