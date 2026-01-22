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
        /// Default constructor for simplified initialization from IfcComparerObjects
        /// </summary>
        public IfcObjectStorage()
        {
        }
    }
}