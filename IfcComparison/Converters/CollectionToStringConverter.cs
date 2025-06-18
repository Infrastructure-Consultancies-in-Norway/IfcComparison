using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace IfcComparison.Converters
{
    public class CollectionToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<string> collection)
            {
                return string.Join(", ", collection);
            }
            
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                // Don't split by comma during editing, only when focus is lost
                if (parameter is bool isEditing && isEditing)
                    return value;
                
                return stringValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            
            return new List<string>();
        }
    }
}