
using System.Collections.Generic;
using Xbim.Ifc4.Interfaces;

namespace IfcComparison.Models
{
    public class IfcComparerResult
    {
        // Ilogger _logger = LogManager.GetLogger("IfcComparerResult");


        public List<IIfcObject> OldObjectsNotInNew { get; set; }
        public List<IIfcObject> NewObjectsNotInOld { get; set; }





        public IfcComparerResult() 
        { 
        }
    }
}
