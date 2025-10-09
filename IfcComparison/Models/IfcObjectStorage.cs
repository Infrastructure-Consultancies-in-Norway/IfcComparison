using IfcComparison.Enumerations;
using IfcComparison.Logging;
using IfcComparison.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO.Xml.BsConf;

namespace IfcComparison.Models
{
    public class IfcObjectStorage
    {
        private readonly ILogger<IfcObjectStorage> _logger;

        public Dictionary<IfcGloballyUniqueId, IIfcObject> IfcObjects { get; set; }
        public string ComparisonId { get; set; } = string.Empty;
        public IIfcPropertySet PropertySet { get; set; }
        private readonly IfcStore _ifcModel;
        private readonly IfcEntity _ifcEntity;

        public IfcObjectStorage()
        {
        }

        public IfcObjectStorage(IIfcPropertySet ifcPropertySet, IfcStore ifcModel, IfcEntity ifcEntity, 
            Dictionary<int, List<IIfcRelDefinesByProperties>> relationshipCache)
        {
            _ifcModel = ifcModel;
            _ifcEntity = ifcEntity;
            PropertySet = ifcPropertySet;

            _logger = LoggingService.CreateLogger<IfcObjectStorage>();
            _logger.LogDebug("IfcObjectStorage initialized with PropertySet: {Name}, Entity: {Entity}",
                ifcPropertySet?.Name ?? "null",
                ifcEntity?.Entity ?? "null");

            IfcObjects = new Dictionary<IfcGloballyUniqueId, IIfcObject>();
            if (ifcPropertySet != null)
            {
                // PERFORMANCE IMPROVEMENT: Use cached relationships instead of querying all relationships
                var entityLabel = ((IPersistEntity)ifcPropertySet).EntityLabel;
                var matchingRelationships = relationshipCache.ContainsKey(entityLabel) 
                    ? relationshipCache[entityLabel] 
                    : new List<IIfcRelDefinesByProperties>();
                
                _logger.LogDebug("Found {Count} cached relationships with matching PropertySet EntityLabel: {Label}",
                    matchingRelationships.Count, entityLabel);

                // Get the target type from the ifcEntity string (get this BEFORE processing objects)
                var targetType = IfcTools.GetInterfaceType(_ifcEntity.Entity);
                _logger.LogDebug("Target type for filtering: {Type}", targetType?.Name ?? "null");

                // Get all related objects from matching relationships, filtering by type immediately
                var filteredObjects = matchingRelationships
                    .SelectMany(rel => rel.RelatedObjects)
                    .Where(obj => targetType != null && targetType.IsInstanceOfType(obj))
                    .Cast<IIfcObject>()
                    .ToList();
                _logger.LogDebug("After filtering by type, found {Count} objects", filteredObjects.Count);
                    
                // Add filtered objects to the dictionary
                foreach (var obj in filteredObjects)
                {
                    IfcObjects[obj.GlobalId] = obj;
                }
                
                // Populate comparison ID if needed
                if (filteredObjects.Any() && _ifcEntity.ComparisonMethod != nameof(ComparisonEnumeration.Identifier))
                {
                    var success = PopulateComparisonId(filteredObjects[0]);
                    _logger.LogDebug("Populated ComparisonId: {Success}, Value: {Value}", 
                        success, ComparisonId);
                }
            }
            else
            {
                _logger.LogWarning("PropertySet is null, cannot retrieve related objects");
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