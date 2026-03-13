using IfcComparison.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace IfcComparison.Converters
{
    /// <summary>
    /// Converts between interface names (e.g., "IIfcBuildingElementProxy") and display names
    /// (e.g., "IfcBuildingElementProxy"). Supports comma-separated lists and the "*" wildcard.
    /// </summary>
    public class InterfaceNameConverter : IValueConverter
    {
        /// <summary>
        /// Convert from stored interface name(s) to display name(s).
        /// "*" stays as "*". Comma-separated lists have each token converted.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string raw)
            {
                if (raw.Trim() == "*")
                    return "*";

                if (raw.Contains(','))
                {
                    var parts = raw.Split(',')
                        .Select(p => IfcTools.InterfaceNameToDisplayName(p.Trim()))
                        .Where(p => !string.IsNullOrEmpty(p));
                    return string.Join(", ", parts);
                }

                return IfcTools.InterfaceNameToDisplayName(raw);
            }
            return value;
        }

        /// <summary>
        /// Convert from display name(s) back to stored interface name(s).
        /// "*" stays as "*". Comma-separated lists have each token converted.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string raw)
            {
                if (raw.Trim() == "*")
                    return "*";

                if (raw.Contains(','))
                {
                    var parts = raw.Split(',')
                        .Select(p => IfcTools.DisplayNameToInterfaceName(p.Trim()))
                        .Where(p => !string.IsNullOrEmpty(p));
                    return string.Join(",", parts);
                }

                return IfcTools.DisplayNameToInterfaceName(raw);
            }
            return value;
        }
    }
}
