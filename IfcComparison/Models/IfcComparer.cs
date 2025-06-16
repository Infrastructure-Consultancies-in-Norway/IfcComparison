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
        public IfcEntity Entity { get; }
        public IfcComparerResult IfcComparisonResult { get; private set; } = new IfcComparerResult();
        public IfcWriter IfcWriter { get; set; }

        // Private constructor for the factory method
        private IfcComparer(IfcStore oldModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, IfcEntity entity)
        {
            OldModel = oldModel;
            NewModelQA = newModelQA;
            FileNameSaveAs = fileNameSaveAs;
            TransactionText = transactionText;
            Entity = entity;
        }

        // Public async factory method
        public static async Task<IfcComparer> CreateAsync(IfcStore oldModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, IfcEntity entity)
        {
            var instance = new IfcComparer(oldModel, newModelQA, fileNameSaveAs, transactionText, entity);
            await instance.InitializeAsync();
            return instance;
        }

        private async Task InitializeAsync()
        {
            // Start both initializations in parallel
            var oldObjectsTask = IfcComparerObjects.CreateAsync(OldModel, Entity);
            var newObjectsTask = IfcComparerObjects.CreateAsync(NewModelQA, Entity);

            // Wait for both to complete
            await Task.WhenAll(oldObjectsTask, newObjectsTask);

            // Assign the results
            OldObjects = await oldObjectsTask;
            NewObjects = await newObjectsTask;
        }

        // Method to compare the old and new objects based on the entity's comparison method and operator
        public async Task CompareRevisions()
        {

            // Ensure both OldObjects and NewObjects are initialized
            if (OldObjects == null || NewObjects == null)
            {
                throw new InvalidOperationException("OldObjects or NewObjects are not initialized.");
            }
            
            if (Entity == null)
            {
                throw new InvalidOperationException("Entity is not initialized.");
            }

            // Check the comparison method and call the appropriate comparison method
            switch (Entity.ComparisonMethod)
            {
                case nameof(ComparisonEnumeration.Identifier):
                    await CompareByIdentifier();
                    break;
                // Serves as a OR operator for Contains and Exact calling the same method
                case nameof(ComparisonEnumeration.Contains):
                case nameof(ComparisonEnumeration.Exact):
                    await CompareByProperty();
                    break;
                default:
                    throw new NotSupportedException($"Comparison method '{Entity.ComparisonMethod}' is not supported.");
            }

            // Perform the comparison logic here based on Entity.ComparisonMethod and Entity.ComparisonOperator
            // This is a placeholder for the actual comparison logic
            // You can implement the logic based on your requirements
            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic

            if (IfcWriter != null)
            {
                await Task.Run(() => IfcWriter.WriteToFileAsync(NewModelQA)); // Call the IfcWriter to write the results to a file asynchronously
            }


        }


        private async Task CompareByProperty()
        {
            var ifcComparerResult = new IfcComparerResult();

            // Get the comparison operator from the entity
            var comparisonOperator = Entity.ComparisonOperator;

            // Run the comparison tasks in parallel
            var oldObjectsNotInNew = CheckIfIfcObjectsAreInIfcObjects(OldObjects, NewObjects, comparisonOperator);
            var newObjectsNotInOld = CheckIfIfcObjectsAreInIfcObjects(NewObjects, OldObjects, comparisonOperator);


            // First we will check the
            var propertyCompareResult = PropertyCompare(NewObjects, OldObjects, comparisonOperator);

            // Wait for tasks to complete
            await Task.WhenAll(oldObjectsNotInNew, newObjectsNotInOld, propertyCompareResult);

            // Add the results to the IfcComparerResult
            ifcComparerResult.OldObjectsNotInNew = await oldObjectsNotInNew;
            ifcComparerResult.NewObjectsNotInOld = await newObjectsNotInOld;
            ifcComparerResult.ComparedIfcObjects = await propertyCompareResult;

            IfcWriter = new IfcWriter(ifcComparerResult, NewModelQA.SchemaVersion, FileNameSaveAs);





            // Placeholder for comparison logic by property set
            // Implement the logic based on your requirements
            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
        }

        private async Task<Dictionary<IIfcObject, Dictionary<string, string>>> PropertyCompare(IfcComparerObjects newObjects, IfcComparerObjects oldObjects, string comparisonOperator)
        {
            var result = new Dictionary<IIfcObject, Dictionary<string, string>>();

            foreach (var newObject in newObjects.IfcObjects)
            {
                // result

                //var isComopared = false;
                var newIdNomValue = GetPropertyNominalValue(comparisonOperator, newObject);

                // Use the nominal value to compare with the the Old
                foreach (var oldObject in oldObjects.IfcObjects)
                {
                    var oldIdNomValue = GetPropertyNominalValue(comparisonOperator, oldObject);
                    // Compare the nominal values based on the comparison operator
                    if (newIdNomValue == oldIdNomValue)
                    {
                        // Do the property comparison logic and assigne the result
                        // to a new propertySet in the newModelQA

                        // Get the newropertySet from the newObject
                        var newPropertySet = newObject.IfcObjects.Values.FirstOrDefault().Item2.HasProperties;
                        // Get the oldPropertySet from the oldObject
                        var oldPropertySet = oldObject.IfcObjects.Values.FirstOrDefault().Item2.HasProperties;

                        // Create a new property set in the newModelQA
                        var propsertySetResult = CompareQAPropertySets(newPropertySet, oldPropertySet);

                        // Add the result to the dictionary
                        foreach (var ifcObj in newObject.IfcObjects)
                        {
                            // Check if the IfcObject already exists in the result dictionary
                            if (!result.ContainsKey(ifcObj.Value.Item1))
                            {
                                // If not, create a new entry with the IfcObject and an empty dictionary
                                result[ifcObj.Value.Item1] = propsertySetResult;
                            }
                            else
                            {
                                // Log the fact that the object already exists
                                // Have to implement a logging mechanism if later
                            }

                        }
                    }
                }

            }

            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
            return result;

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
            // Get the nominal value of the property set based on the comparison operator
            var dictValue = newObject.IfcObjects.Values
                .FirstOrDefault(val => val.Item2.HasProperties.Any(prop => prop.Name.ToString().Contains(comparisonOperator)));

            // Check if the dictValue is null or empty
            if (dictValue == default || dictValue.Item2 == null || !dictValue.Item2.HasProperties.Any())
            {
                return string.Empty; // Return empty string if no properties found
            }

            var idValue = (IIfcPropertySingleValue)dictValue.Item2.HasProperties.FirstOrDefault(prop => prop.Name.ToString().Contains(comparisonOperator));
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
        private async Task<List<IfcObjectStorage>> CheckIfIfcObjectsAreInIfcObjects(IfcComparerObjects oldObjects, IfcComparerObjects newObjects, string comparisonOperator)
        {
            var result = new List<IfcObjectStorage>();

            foreach (var oldObject in oldObjects.IfcObjects)
            {
                var oldIdNomValue = GetPropertyNominalValue(comparisonOperator, oldObject);

                bool shouldAdd = true;
                foreach (var newObject in newObjects.IfcObjects)
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

            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
            return result;

        }

        private async Task<IfcComparerResult> CompareByIdentifier()
        {
            var ifcComparerResult = new IfcComparerResult();





            // Placeholder for comparison logic by identifier
            // Implement the logic based on your requirements
            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
            return ifcComparerResult;

        }




    }
}
