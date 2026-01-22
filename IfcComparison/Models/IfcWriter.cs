using IfcComparison.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;

namespace IfcComparison.Models
{
    public class IfcWriter
    {
        public IfcComparerResult IfcComparisonResult { get; private set; }
        public XbimSchemaVersion IfcSchemaVersion { get; private set; }
        public string FilePath { get; set; }
        private readonly ILogger<IfcWriter> _logger;


        public IfcWriter(IfcComparerResult ifcComparerResult, XbimSchemaVersion xbimSchemaVersion, string filePath) 
        { 
            IfcComparisonResult = ifcComparerResult;
            IfcSchemaVersion = xbimSchemaVersion;
            FilePath = filePath;
            _logger = LoggingService.CreateLogger<IfcWriter>();
        }

        public async Task<bool> WriteToFileAsync(IfcStore ifcModelQA, Dictionary<Xbim.Ifc4.Interfaces.IIfcObject, string> objectPSetMap)
        {
            bool isWritten = false;
            int processedCount = 0;
            int errorCount = 0;
            const int batchSize = 500; // Commit every 500 objects to avoid ESENT memory issues

            return await Task.Run(() =>
            {
                try
                {
                    var totalCount = IfcComparisonResult.ComparedIfcObjects.Count;
                    _logger.LogInformation($"Starting to write {totalCount} objects to file in batches of {batchSize}...");
                    
                    var objectsList = IfcComparisonResult.ComparedIfcObjects.ToList();
                    int batchNumber = 0;

                    for (int i = 0; i < objectsList.Count; i += batchSize)
                    {
                        batchNumber++;
                        var batch = objectsList.Skip(i).Take(batchSize).ToList();
                        
                        using (var transaction = ifcModelQA.BeginTransaction($"Save IFC Comparison Result - Batch {batchNumber}"))
                        {
                            foreach (var ifcObj in batch)
                            {
                                var ifcObject = ifcObj.Key;
                                var properties = ifcObj.Value;
                                
                                try
                                {
                                    // Validate that the object belongs to this model
                                    var entityLabel = ((IPersistEntity)ifcObject).EntityLabel;
                                    var modelEntity = ifcModelQA.Instances[entityLabel];
                                    
                                    if (modelEntity == null)
                                    {
                                        _logger.LogWarning($"Object with EntityLabel {entityLabel} not found in target model, skipping...");
                                        errorCount++;
                                        continue;
                                    }

                                    // Use the correct PSetName for this object or fallback to a default
                                    string pSetName = "QA_PSET";
                                    if (objectPSetMap.TryGetValue(ifcObject, out var mappedPSetName))
                                    {
                                        pSetName = mappedPSetName;
                                    }

                                    IfcTools.GeneratePropertySetIfc(ifcModelQA, ifcObject, properties, pSetName, IfcSchemaVersion);
                                    processedCount++;
                                    
                                    if (processedCount % 1000 == 0)
                                    {
                                        _logger.LogInformation($"Processed {processedCount}/{totalCount} objects...");
                                    }
                                }
                                catch (Exception objEx)
                                {
                                    var entityLabel = ((IPersistEntity)ifcObject).EntityLabel;
                                    _logger.LogWarning($"Error processing object {entityLabel}: {objEx.Message}");
                                    errorCount++;
                                    // Continue with next object instead of failing entirely
                                }
                            }
                            
                            transaction.Commit();
                            _logger.LogDebug($"Committed batch {batchNumber} with {batch.Count} objects");
                        }
                    }

                    _logger.LogInformation($"All batches committed. Total: {processedCount} objects processed, {errorCount} errors.");
                    _logger.LogInformation($"Saving file to {FilePath}...");
                    ifcModelQA.SaveAs(FilePath);
                    isWritten = true;
                    _logger.LogInformation($"File saved successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error saving IFC file: {ex.Message}");
                    throw new InvalidOperationException($"Error saving IFC file: {ex.Message}", ex);
                }
                return isWritten;
            });
        }

        // Keep the original method for backward compatibility
        public async Task<bool> WriteToFileAsync(IfcStore ifcModelQA, string pSetName)
        {
            bool isWritten = false;
            int processedCount = 0;
            int errorCount = 0;
            const int batchSize = 500; // Commit every 500 objects to avoid ESENT memory issues

            return await Task.Run(() =>
            {
                try
                {
                    var totalCount = IfcComparisonResult.ComparedIfcObjects.Count;
                    _logger.LogInformation($"Starting to write {totalCount} objects to file in batches of {batchSize}...");
                    
                    var objectsList = IfcComparisonResult.ComparedIfcObjects.ToList();
                    int batchNumber = 0;

                    for (int i = 0; i < objectsList.Count; i += batchSize)
                    {
                        batchNumber++;
                        var batch = objectsList.Skip(i).Take(batchSize).ToList();
                        
                        using (var transaction = ifcModelQA.BeginTransaction($"Save IFC Comparison Result - Batch {batchNumber}"))
                        {
                            foreach (var ifcObj in batch)
                            {
                                var ifcObject = ifcObj.Key;
                                var properties = ifcObj.Value;

                                try
                                {
                                    // Validate that the object belongs to this model
                                    var entityLabel = ((IPersistEntity)ifcObject).EntityLabel;
                                    var modelEntity = ifcModelQA.Instances[entityLabel];
                                    
                                    if (modelEntity == null)
                                    {
                                        _logger.LogWarning($"Object with EntityLabel {entityLabel} not found in target model, skipping...");
                                        errorCount++;
                                        continue;
                                    }

                                    IfcTools.GeneratePropertySetIfc(ifcModelQA, ifcObject, properties, pSetName, IfcSchemaVersion);
                                    processedCount++;
                                    
                                    if (processedCount % 1000 == 0)
                                    {
                                        _logger.LogInformation($"Processed {processedCount}/{totalCount} objects...");
                                    }
                                }
                                catch (Exception objEx)
                                {
                                    var entityLabel = ((IPersistEntity)ifcObject).EntityLabel;
                                    _logger.LogWarning($"Error processing object {entityLabel}: {objEx.Message}");
                                    errorCount++;
                                }
                            }
                            
                            transaction.Commit();
                            _logger.LogDebug($"Committed batch {batchNumber} with {batch.Count} objects");
                        }
                    }

                    _logger.LogInformation($"All batches committed. Total: {processedCount} objects processed, {errorCount} errors.");
                    _logger.LogInformation($"Saving file to {FilePath}...");
                    ifcModelQA.SaveAs(FilePath);
                    isWritten = true;
                    _logger.LogInformation($"File saved successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error saving IFC file: {ex.Message}");
                    throw new InvalidOperationException($"Error saving IFC file: {ex.Message}", ex);
                }
                return isWritten;
            });
        }
    }
}
