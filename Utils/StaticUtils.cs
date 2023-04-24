using IfcComparison.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace IfcComparison.Utils
{
    internal static class StaticUtils
    {
        internal static ObservableCollection<ComparisonEnumeration> ComparisonList()
        {
            var ComparisonList = new ObservableCollection<ComparisonEnumeration>(
                Enum.GetValues(typeof(ComparisonEnumeration)).Cast<ComparisonEnumeration>());
            return ComparisonList;
        }


    }
}
