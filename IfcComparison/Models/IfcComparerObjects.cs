using IfcComparison.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcComparison.Models
{
    public class IfcComparerObjects
    {
        public IfcStore IfcComparerModel { get; }
        public IfcEntity Entity { get; }
        public List<IfcObjectStorage> IfcObjects { get; private set; }


        public IfcComparerObjects(IfcStore ifcModel, IfcEntity entity)
        {
            IfcComparerModel = ifcModel;
            Entity = entity;

            InitializeIfcObjects();
        }

        private void InitializeIfcObjects()
        {
            var ifcPropertySets = IfcComparerModel.Instances.OfType<IIfcPropertySet>();
            IfcObjects = new List<IfcObjectStorage>();

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
                IfcObjects.Add(ifcObjectStorage);
            }
        }
    }
}