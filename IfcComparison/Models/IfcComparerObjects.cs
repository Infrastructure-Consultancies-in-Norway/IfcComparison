using IfcComparison.Logging;
using IfcComparison.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

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
            await instance.InitializeIfcObjects();
            return instance;
        }

        private async Task InitializeIfcObjects()
        {
            _logger.LogInformation("Initializing IFC objects for {EntityType}", Entity?.Entity ?? "Unknown");
            
            try
            {
                // Get all property sets from the IfcComparerModel
                var ifcPropertySets = IfcComparerModel.Instances.OfType<IIfcPropertySet>();
                _logger.LogDebug("Found {Count} property sets in the model", ifcPropertySets.Count());

                // Use the property set from the entity, which is a list of string, to filter the objects from ifcPropertySets
                var filteredPropertySets = ifcPropertySets
                    .Where(ps =>
                        !string.IsNullOrEmpty(ps.Name) &&
                        Entity.IfcPropertySets != null &&
                        Entity.IfcPropertySets.Any(set => string.Equals(set, ps.Name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                _logger.LogDebug("Filtered to {Count} property sets that match criteria", filteredPropertySets.Count);

                foreach (var propertySet in filteredPropertySets)
                {
                    // Create a new IfcObjectStorage for each property set and add it to the list
                    _logger.LogTrace("Processing property set: {PropertySetName}", propertySet.Name);
                    var ifcObjectStorage = new IfcObjectStorage(propertySet, IfcComparerModel, Entity);

                    // Check if the IfcObjectStorage is not null before adding it to the list
                    if (ifcObjectStorage != null && ifcObjectStorage.IfcObjects.Count > 0)
                    {
                        _logger.LogDebug("Adding storage with {Count} objects for property set {PropertySetName}", 
                            ifcObjectStorage.IfcObjects.Count, propertySet.Name);
                        IfcStorageObjects.Add(ifcObjectStorage);
                    }
                    else
                    {
                        _logger.LogWarning("No objects found for property set {PropertySetName}", propertySet.Name);
                    }
                }

                _logger.LogInformation("Successfully initialized {Count} IFC storage objects", IfcStorageObjects.Count);
                
                // Add a small delay to ensure this is truly async since the method is async
                // and we don't have any real async operations here
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing IFC objects: {Message}", ex.Message);
                throw;
            }
        }
    }
}