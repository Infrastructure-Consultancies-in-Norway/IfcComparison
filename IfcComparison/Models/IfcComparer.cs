using IfcComparison.Enumerations;
using IfcComparison.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;

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




        }

        private async Task CompareByIdentifier()
        {



            // Placeholder for comparison logic by identifier
            // Implement the logic based on your requirements
            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
        }

        private async Task CompareByProperty()
        {



            // Placeholder for comparison logic by property set
            // Implement the logic based on your requirements
            await Task.Delay(1); // Simulate async operation, replace with actual comparison logic
        }


    }
}
