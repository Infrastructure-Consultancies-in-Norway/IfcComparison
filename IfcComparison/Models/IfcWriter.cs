using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Step21;
using Xbim.Ifc;

namespace IfcComparison.Models
{
    public class IfcWriter
    {
        public IfcComparerResult IfcComparisonResult { get; private set; }
        public XbimSchemaVersion IfcSchemaVersion { get; private set; }
        public string FilePath { get; set; }


        public IfcWriter(IfcComparerResult ifcComparerResult, XbimSchemaVersion xbimSchemaVersion, string filePath) 
        { 
            IfcComparisonResult = ifcComparerResult;
            IfcSchemaVersion = xbimSchemaVersion;
            FilePath = filePath;
        }

        public async Task<bool> WriteToFileAsync(IfcStore ifcModelQA, Dictionary<Xbim.Ifc4.Interfaces.IIfcObject, string> objectPSetMap)
        {
            bool isWritten = false;

            return await Task.Run(() =>
            {
                try
                {
                    using (var transaction = ifcModelQA.BeginTransaction("Save IFC Comparison Result"))
                    {
                        foreach (var ifcObj in IfcComparisonResult.ComparedIfcObjects)
                        {
                            var ifcObject = ifcObj.Key;
                            var properties = ifcObj.Value;
                            
                            // Use the correct PSetName for this object or fallback to a default
                            string pSetName = "QA_PSET";
                            if (objectPSetMap.TryGetValue(ifcObject, out var mappedPSetName))
                            {
                                pSetName = mappedPSetName;
                            }

                            IfcTools.GeneratePropertySetIfc(ifcModelQA, ifcObject, properties, pSetName, IfcSchemaVersion);
                        }
                        transaction.Commit();
                    }

                    // Save the IfcStore to a file with the specified schema version
                    ifcModelQA.SaveAs(FilePath);
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

        // Keep the original method for backward compatibility
        public async Task<bool> WriteToFileAsync(IfcStore ifcModelQA, string pSetName)
        {
            bool isWritten = false;

            return await Task.Run(() =>
            {
                try
                {
                    using (var transaction = ifcModelQA.BeginTransaction("Save IFC Comparison Result"))
                    {
                        foreach (var ifcObj in IfcComparisonResult.ComparedIfcObjects)
                        {
                            var ifcObject = ifcObj.Key;
                            var properties = ifcObj.Value;

                            IfcTools.GeneratePropertySetIfc(ifcModelQA, ifcObject, properties, pSetName, IfcSchemaVersion);
                        }
                        transaction.Commit();
                    }

                    // Save the IfcStore to a file with the specified schema version
                    ifcModelQA.SaveAs(FilePath);
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
