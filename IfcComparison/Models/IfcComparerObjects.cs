using IfcComparison.Logging;
using IfcComparison.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;

namespace IfcComparison.Models
{
    public class IfcComparerObjects
    {
        private readonly ILogger<IfcComparerObjects> _logger;
        public IfcStore IfcComparerModel { get; }
        public IfcEntity Entity { get; }
        public List<IfcObjectStorage> IfcStorageObjects { get; private set; } = new List<IfcObjectStorage>();

        // Private constructor to support the factory pattern
        private IfcComparerObjects(IfcStore ifcModel, IfcEntity entity)
        {
            IfcComparerModel = ifcModel;
            Entity = entity;
            _logger = LoggingService.CreateLogger<IfcComparerObjects>();
            _logger.LogDebug("IfcComparerObjects created for entity: {EntityType}", entity?.Entity ?? "Unknown");
        }

        // Async factory method to create and initialize an instance
        public static async Task<IfcComparerObjects> CreateAsync(IfcStore ifcModel, IfcEntity entity)
        {
            var logger = LoggingService.CreateLogger(typeof(IfcComparerObjects).FullName);
            logger.LogInformation("Creating IfcComparerObjects for entity type: {EntityType}", entity?.Entity ?? "Unknown");
            
            var instance = new IfcComparerObjects(ifcModel, entity);
            await instance.InitializeIfcObjectsOptimized();
            return instance;
        }

        /// <summary>
        /// OPTIMIZED: Single-pass initialization using forward lookups.
        /// Instead of iterating through each property set and querying relationships,
        /// we iterate through relationships once and group by property set.
        /// </summary>
        private async Task InitializeIfcObjectsOptimized()
        {
            _logger.LogInformation("Initializing IFC objects for {EntityType} using OPTIMIZED single-pass method", Entity?.Entity ?? "Unknown");
            
            try
            {
                var requiredPSetNames = Entity?.IfcPropertySets ?? new List<string>();
                var targetType = IfcTools.GetInterfaceType(Entity?.Entity);
                var comparisonOperator = Entity?.ComparisonOperator ?? string.Empty;
                var comparisonMethod = Entity?.ComparisonMethod ?? string.Empty;
                
                _logger.LogDebug("Looking for property sets: {PSets}, Target type: {Type}", 
                    string.Join(", ", requiredPSetNames), targetType?.Name ?? "null");

                // Dictionary to group objects by property set
                // Key: PropertySet, Value: (Objects dictionary, ComparisonId)
                var psetToObjects = new Dictionary<IIfcPropertySet, Dictionary<IfcGloballyUniqueId, IIfcObject>>();
                var psetToComparisonId = new Dictionary<IIfcPropertySet, string>();

                // SINGLE PASS through all relationships
                var allRelationships = IfcComparerModel.Instances.OfType<IIfcRelDefinesByProperties>().ToList();
                _logger.LogDebug("Processing {Count} relationships in single pass", allRelationships.Count);

                foreach (var rel in allRelationships)
                {
                    // Get matching property sets from this relationship
                    var matchingPSets = rel.RelatingPropertyDefinition.PropertySetDefinitions
                        .OfType<IIfcPropertySet>()
                        .Where(ps => !string.IsNullOrEmpty(ps.Name) && 
                                     requiredPSetNames.Any(name => string.Equals(name, ps.Name, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (!matchingPSets.Any())
                        continue;

                    // Get related objects filtered by target type
                    var filteredObjects = rel.RelatedObjects
                        .Where(obj => targetType == null || targetType.IsInstanceOfType(obj))
                        .OfType<IIfcObject>()
                        .ToList();

                    if (!filteredObjects.Any())
                        continue;

                    // For each matching property set, add the objects
                    foreach (var pset in matchingPSets)
                    {
                        if (!psetToObjects.ContainsKey(pset))
                        {
                            psetToObjects[pset] = new Dictionary<IfcGloballyUniqueId, IIfcObject>();
                            
                            // Extract comparison ID from this property set (do it once per pset)
                            if (comparisonMethod != nameof(IfcComparison.Enumerations.ComparisonEnumeration.Identifier))
                            {
                                var comparisonValue = pset.HasProperties
                                    .OfType<IIfcPropertySingleValue>()
                                    .FirstOrDefault(p => p.Name.ToString().Contains(comparisonOperator))
                                    ?.NominalValue?.ToString() ?? string.Empty;
                                psetToComparisonId[pset] = comparisonValue;
                            }
                        }

                        // Add objects to this property set's collection
                        foreach (var obj in filteredObjects)
                        {
                            psetToObjects[pset][obj.GlobalId] = obj;
                        }
                    }
                }

                _logger.LogDebug("Found {Count} unique property sets with matching objects", psetToObjects.Count);

                // Create IfcObjectStorage instances from the grouped data
                foreach (var kvp in psetToObjects)
                {
                    var pset = kvp.Key;
                    var objects = kvp.Value;
                    
                    if (objects.Count > 0)
                    {
                        var storage = new IfcObjectStorage
                        {
                            PropertySet = pset,
                            IfcObjects = objects,
                            ComparisonId = psetToComparisonId.TryGetValue(pset, out var compId) ? compId : string.Empty
                        };
                        
                        IfcStorageObjects.Add(storage);
                    }
                }

                _logger.LogInformation("Successfully initialized {Count} IFC storage objects with optimized single-pass method", IfcStorageObjects.Count);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing IFC objects: {Message}", ex.Message);
                throw;
            }
        }
    }
}