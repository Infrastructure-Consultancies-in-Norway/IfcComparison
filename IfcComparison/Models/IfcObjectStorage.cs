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

namespace IfcComparison.Models
{
    public class IfcObjectStorage
    {
        public Dictionary<IfcGloballyUniqueId, IIfcObject> IfcObjects { get; set; } = new Dictionary<IfcGloballyUniqueId, IIfcObject>();
        public string ComparisonId { get; set; } = string.Empty;
        public IIfcPropertySet PropertySet { get; set; }

        /// <summary>
        /// Per-object cache of the comparisonOperator nominal value, populated by
        /// IfcComparerObjects by scanning ALL PSets on each object (not just the required PSets).
        /// Key: IIfcObject, Value: nominal value string of the comparisonOperator property.
        /// </summary>
        public Dictionary<IIfcObject, string> ObjectComparisonIdCache { get; set; } = new Dictionary<IIfcObject, string>();

        /// <summary>
        /// Default constructor for simplified initialization from IfcComparerObjects
        /// </summary>
        public IfcObjectStorage()
        {
        }
    }
}