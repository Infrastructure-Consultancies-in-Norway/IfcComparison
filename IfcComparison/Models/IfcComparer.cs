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

        // Private constructor for the factory method
        private IfcComparer(IfcStore oldModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, List<IfcEntity> entities)
        {
            OldModel = oldModel;
            NewModelQA = newModelQA;
            FileNameSaveAs = fileNameSaveAs;
            TransactionText = transactionText;
            Entities = entities;
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

            // Create combined result object
            var combinedResult = new IfcComparerResult
            {
                OldObjectsNotInNew = new List<IfcObjectStorage>(),
                NewObjectsNotInOld = new List<IfcObjectStorage>(),
                ComparedIfcObjects = new Dictionary<IIfcObject, Dictionary<string, string>>()
            };

            // Process each entity
            foreach (var entity in Entities)
            {
                // Initialize objects for this specific entity
                var oldObjects = await IfcComparerObjects.CreateAsync(OldModel, entity);
                var newObjects = await IfcComparerObjects.CreateAsync(NewModelQA, entity);

                // Create a temp IfcComparer for this entity to reuse existing comparison logic
                var tempComparer = new IfcComparer(OldModel, NewModelQA, FileNameSaveAs, TransactionText, new List<IfcEntity> { entity })
                {
                    OldObjects = oldObjects,
                    NewObjects = newObjects
                };

                // Process this entity
                await tempComparer.CompareEntityInternal();

                // Combine the results
                combinedResult.OldObjectsNotInNew.AddRange(tempComparer.IfcComparisonResult.OldObjectsNotInNew ?? new List<IfcObjectStorage>());
                combinedResult.NewObjectsNotInOld.AddRange(tempComparer.IfcComparisonResult.NewObjectsNotInOld ?? new List<IfcObjectStorage>());

                // Merge the compared objects dictionaries
                foreach (var kvp in tempComparer.IfcComparisonResult.ComparedIfcObjects ?? new Dictionary<IIfcObject, Dictionary<string, string>>())
                {
                    combinedResult.ComparedIfcObjects[kvp.Key] = kvp.Value;
                }
            }

            // Store the combined results
            IfcComparisonResult = combinedResult;

            // Set up the IfcWriter with combined results
            IfcWriter = new IfcWriter(IfcComparisonResult, NewModelQA.SchemaVersion, FileNameSaveAs);

            // Now write all results to file once
            if (Entities.Any())
            {
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

            // Run the comparison tasks in parallel
            var oldObjectsNotInNew = CheckIfIfcObjectsAreInIfcObjects(OldObjects, NewObjects, comparisonOperator, comparisonMethod);
            var newObjectsNotInOld = CheckIfIfcObjectsAreInIfcObjects(NewObjects, OldObjects, comparisonOperator, comparisonMethod);

            // First we will check the
            var propertyCompareResult = PropertyCompare(NewObjects, OldObjects, comparisonOperator, comparisonMethod);

            // Wait for tasks to complete
            await Task.WhenAll(oldObjectsNotInNew, newObjectsNotInOld, propertyCompareResult);

            // Add the results to the IfcComparerResult
            ifcComparerResult.OldObjectsNotInNew = await oldObjectsNotInNew;
            ifcComparerResult.NewObjectsNotInOld = await newObjectsNotInOld;
            ifcComparerResult.ComparedIfcObjects = await propertyCompareResult;

            IfcWriter = new IfcWriter(ifcComparerResult, NewModelQA.SchemaVersion, FileNameSaveAs);

            // Store the results
            IfcComparisonResult = ifcComparerResult;
        }

        private async Task<Dictionary<IIfcObject, Dictionary<string, string>>> PropertyCompare(IfcComparerObjects newObjects, IfcComparerObjects oldObjects, string comparisonOperator, ComparisonEnumeration comparisonEnumeration)
        {
            var result = new Dictionary<IIfcObject, Dictionary<string, string>>();

            if (comparisonEnumeration != ComparisonEnumeration.Identifier)
            {
                // Create lookup dictionaries to avoid nested loops
                var oldObjectLookup = new Dictionary<string, List<KeyValuePair<IIfcObject, string>>>();

                // Step 1: Build a lookup for old objects based on comparison value
                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    foreach (var oldIfcObj in oldObject.IfcObjects)
                    {
                        var oldIdNomValue = IfcTools.GetComparisonNominalValue(oldIfcObj.Value, comparisonOperator);
                        if (!string.IsNullOrEmpty(oldIdNomValue))
                        {
                            if (!oldObjectLookup.ContainsKey(oldIdNomValue))
                                oldObjectLookup[oldIdNomValue] = new List<KeyValuePair<IIfcObject, string>>();

                            oldObjectLookup[oldIdNomValue].Add(new KeyValuePair<IIfcObject, string>(oldIfcObj.Value, oldIdNomValue));
                        }
                    }
                }

                // Step 2: Process new objects and compare with old ones using the lookup
                foreach (var newObject in newObjects.IfcStorageObjects)
                {
                    foreach (var newIfcObj in newObject.IfcObjects)
                    {
                        var newIdNomValue = IfcTools.GetComparisonNominalValue(newIfcObj.Value, comparisonOperator);

                        // Skip if no nominal value found
                        if (string.IsNullOrEmpty(newIdNomValue))
                            continue;

                        // Check if we have matching old objects
                        if (oldObjectLookup.TryGetValue(newIdNomValue, out var oldMatches))
                        {
                            var newPsets = IfcTools.GetPropertySetsFromObject(newIfcObj.Value, Entities.FirstOrDefault()?.IfcPropertySets);

                            foreach (var oldMatch in oldMatches)
                            {
                                var oldPsets = IfcTools.GetPropertySetsFromObject(oldMatch.Key, Entities.FirstOrDefault()?.IfcPropertySets);

                                // Compare property sets between new and old objects
                                CompareAndAddPropertySets(newIfcObj.Value, newPsets, oldPsets, result);
                            }
                        }
                    }
                }
            }
            else
            {
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
                        }
                    }
                }

                // Step 2: Process new objects and compare with old ones using GlobalId
                foreach (var newObject in newObjects.IfcStorageObjects)
                {
                    foreach (var newIfcObj in newObject.IfcObjects)
                    {
                        // Get the GlobalId string value
                        string globalId = newIfcObj.Key;

                        // Check if this GlobalId exists in the old objects
                        if (oldObjectLookup.TryGetValue(globalId, out var matchingOldObject))
                        {
                            // We have a match by GlobalId
                            var newPsets = IfcTools.GetPropertySetsFromObject(newIfcObj.Value, Entities.FirstOrDefault()?.IfcPropertySets);
                            var oldPsets = IfcTools.GetPropertySetsFromObject(matchingOldObject, Entities.FirstOrDefault()?.IfcPropertySets);

                            // Compare property sets between the new and old objects
                            CompareAndAddPropertySets(newIfcObj.Value, newPsets, oldPsets, result);
                        }
                    }
                }
            }

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
        /// </summary>
        /// <param name="oldObjects"></param>
        /// <param name="newObjects"></param>
        /// <param name="comparisonOperator"></param>
        /// <returns></returns>
        private async Task<List<IfcObjectStorage>> CheckIfIfcObjectsAreInIfcObjects(IfcComparerObjects oldObjects, IfcComparerObjects newObjects, string comparisonOperator, ComparisonEnumeration comparisonEnumeration)
        {
            var result = new List<IfcObjectStorage>();

            if (comparisonEnumeration != ComparisonEnumeration.Identifier)
            {
                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    var oldIdNomValue = GetPropertyNominalValue(comparisonOperator, oldObject);

                    bool shouldAdd = true;
                    foreach (var newObject in newObjects.IfcStorageObjects)
                    {
                        var newIdNomValue = GetPropertyNominalValue(comparisonOperator, newObject);

                        // Compare the old and new objects based on the comparison operator
                        if (oldIdNomValue == newIdNomValue)
                        {
                            // If they match we proceed to next object since we only want to find objects that are not in the other list
                            shouldAdd = false;
                            continue;
                        }
                    }
                    if (shouldAdd)
                    {
                        result.Add(oldObject);
                    }
                }
            }
            else
            {
                // For Identifier comparison, we don't use nominal values
                foreach (var oldObject in oldObjects.IfcStorageObjects)
                {
                    var oldIdNomValues = oldObject.IfcObjects.Keys; // For Identifier comparison, we don't use nominal values
                    bool shouldAdd = true;
                    foreach (var newObject in newObjects.IfcStorageObjects)
                    {
                        var newIdNomValues = newObject.IfcObjects.Keys; // For Identifier comparison, we don't use nominal values
                        // Compare the old and new objects based on the comparison operator
                        if (oldIdNomValues.Intersect(newIdNomValues).Any())
                        {
                            // If they match we proceed to next object since we only want to find objects that are not in the other list
                            shouldAdd = false;
                            continue;
                        }
                    }
                    if (shouldAdd)
                    {
                        result.Add(oldObject);
                    }
                }
            }

            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
            return result;
        }
    }
}
