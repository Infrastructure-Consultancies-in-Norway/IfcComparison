using IfcComparison.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using System.Threading.Tasks;

namespace IfcComparison.Models
{
    public class IfcComparerObjects
    {
        public IfcStore IfcComparerModel { get; }
        public IfcEntity Entity { get; }
        public List<IfcObjectStorage> IfcObjects { get; private set; }

        // Private constructor to support the factory pattern
        private IfcComparerObjects(IfcStore ifcModel, IfcEntity entity)
        {
            IfcComparerModel = ifcModel;
            Entity = entity;
            IfcObjects = new List<IfcObjectStorage>();
        }

        // Async factory method to create and initialize an instance
        public static async Task<IfcComparerObjects> CreateAsync(IfcStore ifcModel, IfcEntity entity)
        {
            var instance = new IfcComparerObjects(ifcModel, entity);
            await instance.InitializeIfcObjects();
            return instance;
        }

        private async Task InitializeIfcObjects()
        {
            // Get all property sets from the IfcComparerModel
            var ifcPropertySets = IfcComparerModel.Instances.OfType<IIfcPropertySet>();

            // Use the property set from the entity, which is a list of string, to filter the objects from ifcPropertySets
            var filteredPropertySets = ifcPropertySets
                .Where(ps =>
                    !string.IsNullOrEmpty(ps.Name) &&
                    Entity.IfcPropertySets != null &&
                    Entity.IfcPropertySets.Any(set => string.Equals(set, ps.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var propertySet in filteredPropertySets)
            {
                // Create a new IfcObjectStorage for each property set and add it to the list
                var ifcObjectStorage = new IfcObjectStorage(propertySet, IfcComparerModel, Entity.Entity);

                // Check if the IfcObjectStorage is not null before adding it to the list
                if (ifcObjectStorage != null && ifcObjectStorage.IfcObjects.Count > 0)
                    IfcObjects.Add(ifcObjectStorage);
            }

            // Add a small delay to ensure this is truly async since the method is async
            // and we dont have any real async operations here
            await Task.Delay(1);
        }
    }
}