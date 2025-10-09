using IfcComparison.Enumerations;
using IfcComparison.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.UtilityResource;
using Microsoft.Extensions.Logging;
using IfcComparison.Logging;

namespace IfcComparison.Models
{
    public class IfcComparer
    {
        public IfcComparerObjects OldObjects { get; set; }
        public IfcComparerObjects NewObjects { get; set; }
        public IfcStore OldModel { get; }
        public IfcStore NewModelQA { get; }
        public string FileNameSaveAs { get; }
        public string TransactionText { get; }
        public List<IfcEntity> Entities { get; }
        public IfcComparerResult IfcComparisonResult { get; private set; } = new IfcComparerResult();
        public IfcWriter IfcWriter { get; set; }
        private Dictionary<IIfcObject, Dictionary<string, string>> AllComparedObjects { get; set; } = new Dictionary<IIfcObject, Dictionary<string, string>>();
        private readonly ILogger<IfcComparer> _logger;

        // Private constructor for the factory method
        private IfcComparer(IfcStore oldModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, List<IfcEntity> entities)
        {
            OldModel = oldModel;
            NewModelQA = newModelQA;
            FileNameSaveAs = fileNameSaveAs;
            TransactionText = transactionText;
            Entities = entities;
            _logger = LoggingService.CreateLogger<IfcComparer>();
        }

        // Public async factory method with List<IfcEntity>
        public static async Task<IfcComparer> CreateAsync(IfcStore oldModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, List<IfcEntity> entities)
        {
            var instance = new IfcComparer(oldModel, newModelQA, fileNameSaveAs, transactionText, entities);
            await instance.InitializeAsync();
            return instance;
        }

        private async Task InitializeAsync()
        {
            // No need to initialize all entity objects at once
            // They will be processed one by one in CompareAllRevisions
        }

        // New method to process all entities in the list
        public async Task CompareAllRevisions()
        {
            if (Entities == null || !Entities.Any())
            {
                throw new InvalidOperationException("No entities provided for comparison.");
            }

            _logger.LogInformation("Starting comparison of all entities...");
            
            // Create combined result object
            var combinedResult = new IfcComparerResult
            {
                OldObjectsNotInNew = new List<IfcObjectStorage>(),
                NewObjectsNotInOld = new List<IfcObjectStorage>(),
                ComparedIfcObjects = new Dictionary<IIfcObject, Dictionary<string, string>>()
            };

            int totalEntityCount = Entities.Count;

            // PERFORMANCE IMPROVEMENT: Process entities in parallel when safe to do so
            // Note: We need to be careful with IfcStore thread-safety
            var lockObject = new object();
            
            var tasks = Entities.Select(async (entity, index) =>
            {
                var currentEntityIndex = index + 1;
                _logger.LogInformation($"Processing entity {currentEntityIndex}/{totalEntityCount}: {entity.Entity}");
                
                // Initialize objects for this specific entity
                var oldObjects = await IfcComparerObjects.CreateAsync(OldModel, entity);
                var newObjects = await IfcComparerObjects.CreateAsync(NewModelQA, entity);

                _logger.LogInformation($"Found {oldObjects.IfcStorageObjects?.Count ?? 0} objects in old model and {newObjects.IfcStorageObjects?.Count ?? 0} objects in new model");

                // Create a temp IfcComparer for this entity to reuse existing comparison logic
                var tempComparer = new IfcComparer(OldModel, NewModelQA, FileNameSaveAs, TransactionText, new List<IfcEntity> { entity })
                {
                    OldObjects = oldObjects,
                    NewObjects = newObjects
                };

                // Process this entity
                _logger.LogInformation($"Comparing '{entity.Entity}' objects using {entity.ComparisonMethod} method...");
                await tempComparer.CompareEntityInternal();

                // Log comparison results
                int oldNotInNewCount = tempComparer.IfcComparisonResult.OldObjectsNotInNew?.Count ?? 0;
                int newNotInOldCount = tempComparer.IfcComparisonResult.NewObjectsNotInOld?.Count ?? 0;
                int comparedObjectsCount = tempComparer.IfcComparisonResult.ComparedIfcObjects?.Count ?? 0;
                
                _logger.LogInformation($"Results for '{entity.Entity}': {oldNotInNewCount} removed objects, {newNotInOldCount} new objects, {comparedObjectsCount} compared objects");

                return new { Entity = entity, Result = tempComparer.IfcComparisonResult };
            }).ToList();

            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Combine results (thread-safe)
            foreach (var entityResult in results)
            {
                combinedResult.OldObjectsNotInNew.AddRange(entityResult.Result.OldObjectsNotInNew ?? new List<IfcObjectStorage>());
                combinedResult.NewObjectsNotInOld.AddRange(entityResult.Result.NewObjectsNotInOld ?? new List<IfcObjectStorage>());
                
                foreach (var kvp in entityResult.Result.ComparedIfcObjects ?? new Dictionary<IIfcObject, Dictionary<string, string>>())
                {
                    combinedResult.ComparedIfcObjects[kvp.Key] = kvp.Value;
                }
            }

            // Store the combined results
            IfcComparisonResult = combinedResult;

            // Log final totals
            _logger.LogInformation($"Comparison complete. Total results: " + 
                $"{combinedResult.OldObjectsNotInNew?.Count ?? 0} removed objects, " +
                $"{combinedResult.NewObjectsNotInOld?.Count ?? 0} new objects, " +
                $"{combinedResult.ComparedIfcObjects?.Count ?? 0} compared objects");

            // Set up the IfcWriter with combined results
            IfcWriter = new IfcWriter(IfcComparisonResult, NewModelQA.SchemaVersion, FileNameSaveAs);

            // Now write all results to file once
            if (Entities.Any())
            {
                _logger.LogInformation("Writing results to file...");
                
                // Create a dictionary to map objects to their appropriate PSetNames
                var objectPSetMap = new Dictionary<Xbim.Ifc4.Interfaces.IIfcObject, string>();

                // Create combined results with proper PSetName mapping
                foreach (var entity in Entities)
                {
                    // Find objects related to this entity type
                    var entityObjects = IfcComparisonResult.ComparedIfcObjects
                        .Where(kvp => kvp.Key.GetType().GetInterfaces().Any(i => i.Name == entity.Entity))
                        .Select(kvp => kvp.Key);

                    // Associate each object with its correct PSetName
                    foreach (var obj in entityObjects)
                    {
                        objectPSetMap[obj] = entity.PSetName;
                    }
                }

                // Pass the mapping to IfcWriter
                await IfcWriter.WriteToFileAsync(NewModelQA, objectPSetMap);
                _logger.LogInformation("Results successfully written to file");
            }
        }

        // Private method to compare a single entity (reused from original CompareRevisions logic)
        private async Task CompareEntityInternal()
        {
            // Ensure both OldObjects and NewObjects are initialized
            if (OldObjects == null || NewObjects == null)
            {
                throw new InvalidOperationException("OldObjects or NewObjects are not initialized.");
            }

            var entity = Entities.FirstOrDefault();
            if (entity == null)
            {
                throw new InvalidOperationException("Entity is not initialized.");
            }

            // Check the comparison method and call the appropriate comparison method
            switch (entity.ComparisonMethod)
            {
                case nameof(ComparisonEnumeration.Identifier):
                case nameof(ComparisonEnumeration.Contains):
                case nameof(ComparisonEnumeration.Exact):
                    _logger.LogInformation($"Using comparison method: {entity.ComparisonMethod}");
                    await Compare();
                    break;
                default:
                    throw new NotSupportedException($"Comparison method '{entity.ComparisonMethod}' is not supported.");
            }
        }

        // Original CompareRevisions method maintained for backward compatibility
        public async Task CompareRevisions()
        {
            // Ensure both OldObjects and NewObjects are initialized
            if (OldObjects == null || NewObjects == null)
            {
                throw new InvalidOperationException("OldObjects or NewObjects are not initialized.");
            }

            var entity = Entities.FirstOrDefault();
            if (entity == null)
            {
                throw new InvalidOperationException("Entity is not initialized.");
            }

            // Check the comparison method and call the appropriate comparison method
            switch (entity.ComparisonMethod)
            {
                case nameof(ComparisonEnumeration.Identifier):
                case nameof(ComparisonEnumeration.Contains):
                case nameof(ComparisonEnumeration.Exact):
                    await Compare();
                    break;
                default:
                    throw new NotSupportedException($"Comparison method '{entity.ComparisonMethod}' is not supported.");
            }

            if (IfcWriter != null)
            {
                await Task.Run(() => IfcWriter.WriteToFileAsync(NewModelQA, entity.PSetName));
            }
        }

        // Keep the existing Compare method as it is
        private async Task Compare()
        {
            var ifcComparerResult = new IfcComparerResult();

            // Get the comparison operator from the entity
            var entity = Entities.FirstOrDefault();
            var comparisonOperator = entity.ComparisonOperator;
            // Specify the type explicitly for Enum.Parse
            var comparisonMethod = Enum.Parse<ComparisonEnumeration>(entity.ComparisonMethod);

            _logger.LogInformation($"Starting comparison using operator '{comparisonOperator}' and method '{comparisonMethod}'");

            // Run the comparison tasks in parallel
            _logger.LogInformation("Checking for objects not in the other model...");
            var oldObjectsNotInNew = CheckIfIfcObjectsAreInIfcObjects(OldObjects, NewObjects, comparisonOperator, comparisonMethod, "new");
            var newObjectsNotInOld = CheckIfIfcObjectsAreInIfcObjects(NewObjects, OldObjects, comparisonOperator, comparisonMethod, "old");

            // First we will check the properties
            _logger.LogInformation("Comparing properties between models...");
            var propertyCompareResult = PropertyCompare(NewObjects, OldObjects, comparisonOperator, comparisonMethod);

            // Wait for tasks to complete
            _logger.LogInformation("Waiting for all comparison tasks to complete...");
            await Task.WhenAll(oldObjectsNotInNew, newObjectsNotInOld, propertyCompareResult);

            // Add the results to the IfcComparerResult
            ifcComparerResult.OldObjectsNotInNew = await oldObjectsNotInNew;
            ifcComparerResult.NewObjectsNotInOld = await newObjectsNotInOld;
            ifcComparerResult.ComparedIfcObjects = await propertyCompareResult;

            _logger.LogInformation($"Comparison results: {ifcComparerResult.OldObjectsNotInNew.Count} old objects not in new, " +
                $"{ifcComparerResult.NewObjectsNotInOld.Count} new objects not in old, " +
                $"{ifcComparerResult.ComparedIfcObjects.Count} objects compared");

            IfcWriter = new IfcWriter(ifcComparerResult, NewModelQA.SchemaVersion, FileNameSaveAs);

            // Store the results
            IfcComparisonResult = ifcComparerResult;
        }

        private async Task<Dictionary<IIfcObject, Dictionary<string, string>>> PropertyCompare(IfcComparerObjects newObjects, IfcComparerObjects oldObjects, string comparisonOperator, ComparisonEnumeration comparisonEnumeration)
        {
            _logger.LogInformation($"Starting property comparison using {comparisonEnumeration} method");
            
            var result = new Dictionary<IIfcObject, Dictionary<string, string>>();

            if (comparisonEnumeration != ComparisonEnumeration.Identifier)
            {
                _logger.LogInformation("Using property-based comparison");
                
                // PERFORMANCE IMPROVEMENT: Cache property sets to avoid repeated retrieval
                var oldPropertySetsCache = new Dictionary<IIfcObject, List<IIfcPropertySet>>();
                var newPropertySetsCache = new Dictionary<IIfcObject, List<IIfcPropertySet>>();
                var requiredPSets = Entities.FirstOrDefault()?.IfcPropertySets;
                
                // Create lookup dictionaries to avoid nested loops
                var oldObjectLookup = new Dictionary<string, List<IIfcObject>>();

                // Step 1: Build a lookup for old objects based on comparison value
                _logger.LogInformation("Building lookup dictionary for old objects");
                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    foreach (var oldIfcObj in oldObject.IfcObjects)
                    {
                        var oldIdNomValue = IfcTools.GetComparisonNominalValue(oldIfcObj.Value, comparisonOperator);
                        if (!string.IsNullOrEmpty(oldIdNomValue))
                        {
                            if (!oldObjectLookup.ContainsKey(oldIdNomValue))
                                oldObjectLookup[oldIdNomValue] = new List<IIfcObject>();

                            oldObjectLookup[oldIdNomValue].Add(oldIfcObj.Value);
                            
                            // PERFORMANCE IMPROVEMENT: Cache property sets during lookup building
                            if (!oldPropertySetsCache.ContainsKey(oldIfcObj.Value))
                                oldPropertySetsCache[oldIfcObj.Value] = IfcTools.GetPropertySetsFromObject(oldIfcObj.Value, requiredPSets);
                        }
                    }
                }
                _logger.LogInformation($"Created lookup with {oldObjectLookup.Count} unique values from old model");

                // Step 2: Process new objects and compare with old ones using the lookup
                int matchCount = 0;
                _logger.LogInformation("Comparing new objects against old objects");
                foreach (var newObject in newObjects.IfcStorageObjects)
                {
                    foreach (var newIfcObj in newObject.IfcObjects)
                    {
                        var newIdNomValue = IfcTools.GetComparisonNominalValue(newIfcObj.Value, comparisonOperator);

                        // Skip if no nominal value found
                        if (string.IsNullOrEmpty(newIdNomValue))
                            continue;

                        // PERFORMANCE IMPROVEMENT: Cache new property sets during comparison
                        if (!newPropertySetsCache.ContainsKey(newIfcObj.Value))
                            newPropertySetsCache[newIfcObj.Value] = IfcTools.GetPropertySetsFromObject(newIfcObj.Value, requiredPSets);

                        // Check if we have matching old objects
                        if (oldObjectLookup.TryGetValue(newIdNomValue, out var oldMatches))
                        {
                            var newPsets = newPropertySetsCache[newIfcObj.Value];

                            foreach (var oldMatch in oldMatches)
                            {
                                var oldPsets = oldPropertySetsCache[oldMatch];

                                // Compare property sets between new and old objects
                                CompareAndAddPropertySets(newIfcObj.Value, newPsets, oldPsets, result);
                                matchCount++;
                            }
                        }
                    }
                }
                _logger.LogInformation($"Completed comparison with {matchCount} property set comparisons");
            }
            else
            {
                _logger.LogInformation("Using GlobalId-based comparison");
                
                // PERFORMANCE IMPROVEMENT: Cache property sets for Identifier comparison too
                var oldPropertySetsCache = new Dictionary<IIfcObject, List<IIfcPropertySet>>();
                var newPropertySetsCache = new Dictionary<IIfcObject, List<IIfcPropertySet>>();
                var requiredPSets = Entities.FirstOrDefault()?.IfcPropertySets;
                
                // For Identifier comparison, we'll use the GlobalId directly

                // Step 1: Build a lookup dictionary of old objects by their GlobalIds
                var oldObjectLookup = new Dictionary<string, IIfcObject>();

                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    foreach (var oldIfcObj in oldObject.IfcObjects)
                    {
                        // Use the GlobalId string as the key
                        if (!oldObjectLookup.ContainsKey(oldIfcObj.Key))
                        {
                            oldObjectLookup[oldIfcObj.Key] = oldIfcObj.Value;
                            
                            // PERFORMANCE IMPROVEMENT: Cache property sets during lookup building
                            if (!oldPropertySetsCache.ContainsKey(oldIfcObj.Value))
                                oldPropertySetsCache[oldIfcObj.Value] = IfcTools.GetPropertySetsFromObject(oldIfcObj.Value, requiredPSets);
                        }
                    }
                }
                _logger.LogInformation($"Created lookup with {oldObjectLookup.Count} objects from old model by GlobalId");

                // Step 2: Process new objects and compare with old ones using GlobalId
                int matchCount = 0;
                foreach (var newObject in newObjects.IfcStorageObjects)
                {
                    foreach (var newIfcObj in newObject.IfcObjects)
                    {
                        // Get the GlobalId string value
                        string globalId = newIfcObj.Key;

                        // PERFORMANCE IMPROVEMENT: Cache new property sets during comparison
                        if (!newPropertySetsCache.ContainsKey(newIfcObj.Value))
                            newPropertySetsCache[newIfcObj.Value] = IfcTools.GetPropertySetsFromObject(newIfcObj.Value, requiredPSets);

                        // Check if this GlobalId exists in the old objects
                        if (oldObjectLookup.TryGetValue(globalId, out var matchingOldObject))
                        {
                            // We have a match by GlobalId
                            var newPsets = newPropertySetsCache[newIfcObj.Value];
                            var oldPsets = oldPropertySetsCache[matchingOldObject];

                            // Compare property sets between the new and old objects
                            CompareAndAddPropertySets(newIfcObj.Value, newPsets, oldPsets, result);
                            matchCount++;
                        }
                    }
                }
                _logger.LogInformation($"Completed comparison with {matchCount} GlobalId matches");
            }

            _logger.LogInformation($"Property comparison complete with {result.Count} results");
            await Task.Delay(1); // Simulate async operation
            return result;
        }

        // Helper method to compare and add property sets
        private void CompareAndAddPropertySets(
            IIfcObject newIfcObj,
            List<IIfcPropertySet> newPsets,
            List<IIfcPropertySet> oldPsets,
            Dictionary<IIfcObject, Dictionary<string, string>> result)
        {
            foreach (var newPset in newPsets)
            {
                var oldPset = oldPsets.FirstOrDefault(p => p.Name == newPset.Name);
                if (oldPset == null)
                    continue;

                // Compare the property sets
                var propertySetResult = CompareQAPropertySets(newPset.HasProperties, oldPset.HasProperties);

                // Add or merge into result
                if (!result.ContainsKey(newIfcObj))
                {
                    result[newIfcObj] = propertySetResult;
                }
                else
                {
                    var existingProperties = result[newIfcObj];

                    foreach (var kvp in propertySetResult)
                    {
                        if (!existingProperties.ContainsKey(kvp.Key))
                        {
                            existingProperties[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
        }

        private Dictionary<string, string> CompareQAPropertySets(IItemSet<IIfcProperty> newPropertySet, IItemSet<IIfcProperty> oldPropertySet)
        {
            var result = new Dictionary<string, string>();
            // Compare the properties in the newPropertySet with the oldPropertySet
            foreach (var newProperty in newPropertySet)
            {
                // Get the new Property as single value
                var newPropertySingleValue = newProperty as IIfcPropertySingleValue;
                IIfcPropertySingleValue oldPropertySingleValue = null;
                if (oldPropertySet != null)
                    oldPropertySingleValue = oldPropertySet
                    .OfType<IIfcPropertySingleValue>()
                    .Where(n => n.Name == newPropertySingleValue.Name)
                    .FirstOrDefault();

                var valToWrite = string.Empty;
                if (oldPropertySet != null)
                {
                    if (oldPropertySingleValue != null)
                    {
                        //Null check to avoid errors where NominalValue is Null
                        var sValProp = newPropertySingleValue.NominalValue?.ToString();
                        var oValProp = oldPropertySingleValue.NominalValue?.ToString();
                        //Writes Null if one of the old or new value are null
                        if (sValProp == null || oValProp == null)
                        {
                            valToWrite = "Null";
                        }
                        else if (string.Equals(sValProp, oValProp))
                        {
                            valToWrite = "Equal";
                        }
                        else
                        {
                            valToWrite = $"Changed from \"{oldPropertySingleValue.NominalValue}\" to \"{newPropertySingleValue.NominalValue}\"";
                        }

                    }
                    else
                    {
                        valToWrite = $"Changed from \"<undefined>\" to \"{newPropertySingleValue.NominalValue}\"";
                    }
                }
                else
                {
                    valToWrite = "N/A";
                }

                var propName = newProperty.Name.ToString();

                // Add the property to the result list
                result[propName] = valToWrite;

            }

            return result;
        }

        private static string GetPropertyNominalValue(string comparisonOperator, IfcObjectStorage newObject)
        {
            var idValue = (IIfcPropertySingleValue)newObject.PropertySet.HasProperties.FirstOrDefault(prop => prop.Name.ToString().Contains(comparisonOperator));
            var idNomValue = idValue?.NominalValue?.ToString() ?? string.Empty;
            return idNomValue;
        }

        /// <summary>
        /// Method to check if IfcObjects in the old list are not present in the new list based on a comparison operator.
        /// PERFORMANCE OPTIMIZED: Uses HashSet for O(1) lookups instead of O(n) nested loops
        /// </summary>
        /// <param name="oldObjects"></param>
        /// <param name="newObjects"></param>
        /// <param name="comparisonOperator"></param>
        /// <returns></returns>
        private async Task<List<IfcObjectStorage>> CheckIfIfcObjectsAreInIfcObjects(IfcComparerObjects oldObjects, IfcComparerObjects newObjects, string comparisonOperator, ComparisonEnumeration comparisonEnumeration, string newOld)
        {
            var result = new List<IfcObjectStorage>();

            if (comparisonEnumeration != ComparisonEnumeration.Identifier)
            {
                // PERFORMANCE IMPROVEMENT: Build HashSet of new nominal values ONCE - O(n) instead of O(n²)
                var newNominalValues = new HashSet<string>(
                    newObjects.IfcStorageObjects
                        .Select(obj => GetPropertyNominalValue(comparisonOperator, obj))
                        .Where(val => !string.IsNullOrEmpty(val))
                );

                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    var oldIdNomValue = GetPropertyNominalValue(comparisonOperator, oldObject);
                    
                    // O(1) lookup instead of O(n) loop
                    if (!string.IsNullOrEmpty(oldIdNomValue) && !newNominalValues.Contains(oldIdNomValue))
                    {
                        result.Add(oldObject);
                        _logger.LogInformation($"Object with nominal value '{oldIdNomValue}' not found in {newOld} objects.");
                    }
                }
            }
            else
            {
                // PERFORMANCE IMPROVEMENT: Build HashSet of new GlobalIds ONCE
                var newGlobalIds = new HashSet<string>(
                    newObjects.IfcStorageObjects
                        .SelectMany(obj => obj.IfcObjects.Keys.Select(k => k.ToString()))
                );

                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    var oldIdNomValues = oldObject.IfcObjects.Keys.Select(k => k.ToString());
                    
                    // Check if any old GlobalId exists in new set
                    if (!oldIdNomValues.Any(id => newGlobalIds.Contains(id)))
                    {
                        result.Add(oldObject);
                        _logger.LogInformation($"Object not found in {newOld} objects.");
                    }
                }
            }

            return await Task.FromResult(result);
        }
    }
}
