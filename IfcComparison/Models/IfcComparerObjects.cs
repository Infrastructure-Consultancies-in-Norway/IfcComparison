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
        /// For Contains/Exact comparison methods the comparisonOperator is searched across
        /// ALL PSets on each object (not only the required PSets), so that objects whose
        /// identifier property lives in a different PSet are still matched correctly.
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
                bool usePropertyComparison = comparisonMethod != nameof(IfcComparison.Enumerations.ComparisonEnumeration.Identifier);

                _logger.LogDebug("Looking for property sets: {PSets}, Target type: {Type}",
                    string.Join(", ", requiredPSetNames), targetType?.Name ?? "null");

                // ----------------------------------------------------------------
                // PASS 1: Build objectAllPSets — all PSets per entity-typed object.
                //         (No filter on PSet name — we want every PSet on the object.)
                // ----------------------------------------------------------------
                var objectAllPSets = new Dictionary<IIfcObject, List<IIfcPropertySet>>();

                if (usePropertyComparison && !string.IsNullOrEmpty(comparisonOperator))
                {
                    var allRelationships = IfcComparerModel.Instances.OfType<IIfcRelDefinesByProperties>().ToList();

                    foreach (var rel in allRelationships)
                    {
                        var psetsInRel = rel.RelatingPropertyDefinition.PropertySetDefinitions
                            .OfType<IIfcPropertySet>()
                            .ToList();

                        if (!psetsInRel.Any())
                            continue;

                        foreach (var obj in rel.RelatedObjects
                            .Where(o => targetType == null || targetType.IsInstanceOfType(o))
                            .OfType<IIfcObject>())
                        {
                            if (!objectAllPSets.ContainsKey(obj))
                                objectAllPSets[obj] = new List<IIfcPropertySet>();

                            foreach (var ps in psetsInRel)
                            {
                                if (!objectAllPSets[obj].Contains(ps))
                                    objectAllPSets[obj].Add(ps);
                            }
                        }
                    }

                    _logger.LogDebug("Pass 1 complete: {Count} entity-typed objects found across all PSets", objectAllPSets.Count);
                }

                // ----------------------------------------------------------------
                // PASS 2: For each object, search ALL its PSets for the
                //         comparisonOperator property and cache the result.
                //         Warn (→ file log + UI console) when not found.
                // ----------------------------------------------------------------
                var objectComparisonIdCache = new Dictionary<IIfcObject, string>();

                if (usePropertyComparison && !string.IsNullOrEmpty(comparisonOperator))
                {
                    foreach (var kvp in objectAllPSets)
                    {
                        var obj = kvp.Key;
                        string found = null;

                        foreach (var pset in kvp.Value)
                        {
                            var prop = pset.HasProperties
                                .OfType<IIfcPropertySingleValue>()
                                .FirstOrDefault(p => p.Name.ToString().Contains(comparisonOperator));

                            if (prop?.NominalValue != null)
                            {
                                found = prop.NominalValue.ToString();
                                break;
                            }
                        }

                        if (found != null)
                        {
                            objectComparisonIdCache[obj] = found;
                        }
                        else
                        {
                            // LogWarning routes to both the file logger and the UI console
                            // via ConsoleLoggerProvider (filter: level >= Information).
                            _logger.LogWarning(
                                "ComparisonOperator '{Operator}' not found in any PSet for object {GlobalId} ({EntityType}). Object will be skipped in comparison.",
                                comparisonOperator, obj.GlobalId, obj.GetType().Name);
                        }
                    }

                    _logger.LogDebug("Pass 2 complete: cached comparison ids for {Count}/{Total} objects",
                        objectComparisonIdCache.Count, objectAllPSets.Count);
                }

                // ----------------------------------------------------------------
                // PASS 3: Build psetToObjects using ONLY requiredPSetNames.
                //         This is the same as the original single-pass logic.
                // ----------------------------------------------------------------
                var psetToObjects = new Dictionary<IIfcPropertySet, Dictionary<IfcGloballyUniqueId, IIfcObject>>();
                var psetToComparisonId = new Dictionary<IIfcPropertySet, string>();

                var relationships = IfcComparerModel.Instances.OfType<IIfcRelDefinesByProperties>().ToList();
                _logger.LogDebug("Pass 3: processing {Count} relationships", relationships.Count);

                foreach (var rel in relationships)
                {
                    var matchingPSets = rel.RelatingPropertyDefinition.PropertySetDefinitions
                        .OfType<IIfcPropertySet>()
                        .Where(ps => !string.IsNullOrEmpty(ps.Name) &&
                                     requiredPSetNames.Any(name => string.Equals(name, ps.Name, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (!matchingPSets.Any())
                        continue;

                    var filteredObjects = rel.RelatedObjects
                        .Where(obj => targetType == null || targetType.IsInstanceOfType(obj))
                        .OfType<IIfcObject>()
                        .ToList();

                    if (!filteredObjects.Any())
                        continue;

                    foreach (var pset in matchingPSets)
                    {
                        if (!psetToObjects.ContainsKey(pset))
                        {
                            psetToObjects[pset] = new Dictionary<IfcGloballyUniqueId, IIfcObject>();

                            if (usePropertyComparison)
                            {
                                // Derive the pset-level comparison id from the object cache when possible
                                // (fall back to scanning the PSet directly for the Identifier method)
                                var comparisonValue = pset.HasProperties
                                    .OfType<IIfcPropertySingleValue>()
                                    .FirstOrDefault(p => p.Name.ToString().Contains(comparisonOperator))
                                    ?.NominalValue?.ToString() ?? string.Empty;
                                psetToComparisonId[pset] = comparisonValue;
                            }
                        }

                        foreach (var obj in filteredObjects)
                        {
                            psetToObjects[pset][obj.GlobalId] = obj;
                        }
                    }
                }

                _logger.LogDebug("Pass 3 complete: {Count} unique required-pset entries found", psetToObjects.Count);

                // ----------------------------------------------------------------
                // Build IfcObjectStorage instances from the grouped data.
                // Attach the per-object comparison id cache built in Pass 2.
                // ----------------------------------------------------------------
                foreach (var kvp in psetToObjects)
                {
                    var pset = kvp.Key;
                    var objects = kvp.Value;

                    if (objects.Count > 0)
                    {
                        // Subset the cache to only the objects in this storage entry
                        var subsetCache = new Dictionary<IIfcObject, string>();
                        foreach (var obj in objects.Values)
                        {
                            if (objectComparisonIdCache.TryGetValue(obj, out var cachedId))
                                subsetCache[obj] = cachedId;
                        }

                        var storage = new IfcObjectStorage
                        {
                            PropertySet = pset,
                            IfcObjects = objects,
                            ComparisonId = psetToComparisonId.TryGetValue(pset, out var compId) ? compId : string.Empty,
                            ObjectComparisonIdCache = subsetCache
                        };

                        IfcStorageObjects.Add(storage);
                    }
                }

                _logger.LogInformation("Successfully initialized {Count} IFC storage objects", IfcStorageObjects.Count);

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
