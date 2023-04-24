using IfcComparison.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IfcComparison.Utils;

namespace IfcComparison.ViewModels
{

    public class IfcEntities
    {
        public string IfcEntity { get; set; } 
        public string IfcPropertySet { get; set; }
        public string ComparisonOperator { get; set; }
        public string ComparisonMethod { get; set; }

    }




    //public class IfcEntities
    //{
    //    public IfcEntity<object> IfcEntity { get; set; } = new IfcEntity<object>();
    //    public string IfcPropertySet { get; set; }
    //    public string ComparisonOperator { get; set; }

    //}

    //public class IfcEntity<T>
    //{ 
    //    public T Entity { get; set; }
    //}

}
