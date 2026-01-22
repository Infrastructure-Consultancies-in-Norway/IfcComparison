using System;
using System.IO;

namespace IfcComparison.Tests.Integration.Fixtures
{
    /// <summary>
    /// Fixture for Nordstrand project IFC files.
    /// </summary>
    public class NordstrandIfcFixture : IfcModelFixture
    {
        private const string BaseDirectory = "../../../IfcFiles/Nordstrand";
        
        public NordstrandIfcFixture() : base(
            oldFilePath: Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_03C.ifc"),
            newFilePath: Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_04C.ifc"),
            qaFilePath: Path.Combine(BaseDirectory, "SOS_05NOR_F_KON_OVERGANGSBRU_04C_QA_Test.ifc")
        )
        {
        }
    }
}
