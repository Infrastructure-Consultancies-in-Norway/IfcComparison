using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.UtilityResource;

namespace IfcComparison.Models
{
    public class IfcObjectStorage
    {
        public Dictionary<IfcGloballyUniqueId, (IIfcObject, IIfcPropertySet)> IfcObjects { get; private set; }
        public IIfcPropertySet PropertySet { get; set; }
        private readonly IfcStore _ifcModel;
        private readonly string _ifcEntity;

        public IfcObjectStorage(IIfcPropertySet ifcPropertySet, IfcStore ifcModel, string ifcEntity)
        {
            _ifcModel = ifcModel;
            _ifcEntity = ifcEntity;

            IfcObjects = new Dictionary<IfcGloballyUniqueId, (IIfcObject, IIfcPropertySet)>();
            if (ifcPropertySet != null)
            {
                // Select all RelatedObjects from all IIfcRelDefinesByProperties where RelatingPropertyDefinition matches ifcPropertySet
                var relatedObjects = _ifcModel.Instances.OfType<IIfcRelDefinesByProperties>()
                    .Where(rel => rel.RelatingPropertyDefinition == ifcPropertySet)
                    .SelectMany(rel => rel.RelatedObjects)
                    .ToList();

                var targetType = IfcTools.GetInterfaceType(_ifcEntity);
                foreach (var relObj in relatedObjects)
                {
                    // If no objects found, continue to the next property set
                    if (relatedObjects.Count == 0)
                        continue;

                    if (targetType != null)
                    {
                        // Filter the related objects to find those that match the target type and are of type IIfcObject
                        if (!targetType.IsInstanceOfType(relObj))
                            continue; // Skip if the object is not of the expected type

                        foreach (var ifcObj in relatedObjects)
                        {
                            // Add the object to the dictionary using its GlobalId as the key
                            IfcObjects[ifcObj.GlobalId] = ((IIfcObject)ifcObj, ifcPropertySet);
                        }
                    }
                }
            }
        }
    }

}