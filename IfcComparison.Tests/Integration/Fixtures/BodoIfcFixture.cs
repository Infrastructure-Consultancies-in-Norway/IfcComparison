using System;
using System.IO;

namespace IfcComparison.Tests.Integration.Fixtures
{
    /// <summary>
    /// Fixture for Bodo project IFC files.
    /// Loads OLD_bodl.ifc and NEW_bodl.ifc once for all tests in the class.
    /// </summary>
    public class BodoIfcFixture : IfcModelFixture
    {
        private const string BaseDirectory = "../../../IfcFiles/Bodo";
        
        public BodoIfcFixture() : base(
            oldFilePath: Path.Combine(BaseDirectory, "OLD_bodl.ifc"),
            newFilePath: Path.Combine(BaseDirectory, "NEW_bodl.ifc"),
            qaFilePath: Path.Combine(BaseDirectory, "NEW_bodl_QA_Test.ifc")
        )
        {
        }
    }
}
