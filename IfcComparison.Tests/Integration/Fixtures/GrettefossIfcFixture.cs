using System;
using System.IO;

namespace IfcComparison.Tests.Integration.Fixtures
{
    /// <summary>
    /// Fixture for Grettefoss project IFC files.
    /// </summary>
    public class GrettefossIfcFixture : IfcModelFixture
    {
        private const string BaseDirectory = "../../../IfcFiles/Grettefoss";
        
        public GrettefossIfcFixture() : base(
            oldFilePath: Path.Combine(BaseDirectory, "f-bru_K100 Grettefoss III_RevD.ifc"),
            newFilePath: Path.Combine(BaseDirectory, "f-bru_K100 Grettefoss III_RevE_v01.ifc"),
            qaFilePath: Path.Combine(BaseDirectory, "f-bru_K100 Grettefoss III_RevE_v01_QA_Test.ifc")
        )
        {
        }
    }
}
