using IfcComparison.Enumerations;
using IfcComparison.ViewModels;
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
        public Dictionary<IfcGloballyUniqueId, IIfcObject> IfcObjects { get; set; }
        public string ComparisonId { get; set; } = string.Empty;
        public IIfcPropertySet PropertySet { get; set; }
        //public IIfcPropertySet PropertySet { get; set; }
        private readonly IfcStore _ifcModel;
        private readonly IfcEntity _ifcEntity;

        public IfcObjectStorage()
        {
        }

        public IfcObjectStorage(IIfcPropertySet ifcPropertySet, IfcStore ifcModel, IfcEntity ifcEntity)
        {
            _ifcModel = ifcModel;
            _ifcEntity = ifcEntity;
            PropertySet = ifcPropertySet;

            IfcObjects = new Dictionary<IfcGloballyUniqueId, IIfcObject>();
            if (ifcPropertySet != null)
            {
                // Select all RelatedObjects from all IIfcRelDefinesByProperties where RelatingPropertyDefinition matches ifcPropertySet
                var relatedObjects = _ifcModel.Instances.OfType<IIfcRelDefinesByProperties>()
                    .Where(rel => rel.RelatingPropertyDefinition == ifcPropertySet)
                    .SelectMany(rel => rel.RelatedObjects)
                    .ToList();

#if DEBUG
                if (ifcPropertySet.Name == "SOS-KON_Felles")
                {
                    // If relatedObjects.Count > 0 get the objects of type IIfcReinforcingBar and log the count
                    var reinforcingBars = relatedObjects.OfType<IIfcReinforcingBar>().ToList();
                    if (reinforcingBars.Count > 0)
                    {
                        ;
                    }

                }
#endif


                // Get the target type from the ifcEntity string
                var targetType = IfcTools.GetInterfaceType(_ifcEntity.Entity);
                foreach (var relObj in relatedObjects)
                {
                    // If no objects found, continue to the next property set
                    if (relatedObjects.Count == 0)
                        continue;

                    if (targetType != null)
                    {
                        // Filter the related objects to find those that match the target type and are of type targetType
                        if (!targetType.IsInstanceOfType(relObj))
                            continue; // Skip if the object is not of the expected type

                        var isPopulated = false;
                        foreach (var ifcObj in relatedObjects)
                        {
                            // Add the object to the dictionary using its GlobalId as the key
                            IfcObjects[ifcObj.GlobalId] = (IIfcObject)ifcObj;
                            
                            // 
                            if (isPopulated)
                                continue;
                            // Populate the Comparison Id
                            if (_ifcEntity.ComparisonMethod != nameof(ComparisonEnumeration.Identifier))
                            {
                                isPopulated = PopulateComparisonId((IIfcObject)ifcObj);
                            }
                        }
                    }
                }
            }
        }

        private bool PopulateComparisonId(IIfcObject ifcObject)
        {
            var result = false;
            // Loop all properties on the ifcObject to get the name of the property
            var compOperator = _ifcEntity.ComparisonOperator;
            
            // Get all PropertySet on the object
            var objPsets = ifcObject.IsDefinedBy.OfType<IIfcRelDefinesByProperties>()
                .SelectMany(rel => rel.RelatingPropertyDefinition.PropertySetDefinitions)
                .OfType<IIfcPropertySet>();

            // Loop through all properties in the PropertySets and check if the name contains the ComparisonOperator and return the first nominal value found.
            var propertyNomVal = objPsets
                .SelectMany(ps => ps.HasProperties)
                .Where(prop => prop is IIfcPropertySingleValue)
                .Cast<IIfcPropertySingleValue>()
                .FirstOrDefault(prop => prop.Name.ToString().Contains(compOperator));

            ComparisonId = propertyNomVal?.NominalValue?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(ComparisonId))
            {
                result = true;
            }

            return result;
        }

    }

}