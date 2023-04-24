using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc2x3.StructuralElementsDomain;
using Xbim.Ifc2x3.Interfaces;

namespace IfcComparison.Models
{
    internal class CastIfcEnitity
    {

        public Type TypeOfObject { get; set; }
        public List<object> Instances { get; set; }
        public IfcStore Model { get; set; }

        public CastIfcEnitity(IfcStore model, Type type)
        {
            TypeOfObject = type;
            Model = model;
        }

        //private void IfcEntities()
        //{
        //    Instances = new List<object>();

        //    if (TypeOfObject is IIfcReinforcingBar)
        //    {
        //        Instances = Model.Instances.OfType<IIfcReinforcingBar>().ToList() as IIfcReinforcingBar;
        //    }
        //    else
        //    {
        //        Instances = null;
        //    }
            
        //}



    }
}
