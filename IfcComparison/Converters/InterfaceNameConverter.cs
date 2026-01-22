using IfcComparison.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IfcComparison.Converters
{
    /// <summary>
    /// Converts between interface names (e.g., "IIfcBuildingElementProxy") and display names (e.g., "IfcBuildingElementProxy")
    /// Used for displaying entity names in the UI without the leading "I"
    /// </summary>
    public class InterfaceNameConverter : IValueConverter
    {
        /// <summary>
        /// Convert from interface name to display name (removes leading "I")
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string interfaceName)
            {
                return IfcTools.InterfaceNameToDisplayName(interfaceName);
            }
            return value;
        }

        /// <summary>
        /// Convert from display name to interface name (adds leading "I")
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string displayName)
            {
                return IfcTools.DisplayNameToInterfaceName(displayName);
            }
            return value;
        }
    }
}
