using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;

namespace IfcComparison.Models
{
    public class IfcWriter
    {
        public IfcComparerResult IfcComparisonResult { get; private set; }
        public IfcWriter(IfcComparerResult ifcComparerResult) 
        { 
            IfcComparisonResult = ifcComparerResult;
        }

        public async Task<bool> WriteToFileAsync(IfcStore ifcModelQA)
        {
            bool isWritten = false;

            return await Task.Run(() =>
            {
                try
                {
                    // Save the IfcStore to a file
                    //ifcModelQA.SaveAs(IfcComparisonResult.FileNameSaveAs, Xbim.IO.IfcStorageType.Ifc, Xbim.IO.IfcStorageVersion.Ifc4X3);
                    isWritten = true;
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., log them)
                    throw new InvalidOperationException($"Error saving IFC file: {ex.Message}", ex);
                }
                return isWritten;
            });

        }

    }
}
