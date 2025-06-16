
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;

namespace IfcComparison.Models
{
    public class IfcComparerResult
    {
        // Ilogger _logger = LogManager.GetLogger("IfcComparerResult");


        public List<IfcObjectStorage> OldObjectsNotInNew { get; set; }
        public List<IfcObjectStorage> NewObjectsNotInOld { get; set; }
        public Dictionary<IIfcObject, Dictionary<string, string>> ComparedIfcObjects { get; internal set; }
        

        public IfcComparerResult() 
        { 
        }
    }
}
