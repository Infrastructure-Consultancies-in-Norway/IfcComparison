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
        // Keep track of objects currently being edited
        public static Dictionary<object, string> EditingCache = new Dictionary<object, string>();
        public static object CurrentlyEditedObject = null;
        public static bool IsEditing = false;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If we're editing this specific object, return the cached value
            if (IsEditing && CurrentlyEditedObject != null && 
                value is IEnumerable<string> && EditingCache.ContainsKey(CurrentlyEditedObject))
            {
                return EditingCache[CurrentlyEditedObject];
            }
            
            // Normal conversion for display
            if (value is IEnumerable<string> collection)
            {
                return string.Join(", ", collection);
            }
            
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            
            // During active editing, just store the text
            if (IsEditing && CurrentlyEditedObject != null)
            {
                EditingCache[CurrentlyEditedObject] = stringValue ?? string.Empty;
                
                // Use the existing collection or create a new one when editing
                return new List<string>(); // Return empty placeholder during editing
            }
            
            // When editing is complete, split by commas
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            
            return new List<string>();
        }

        // Helper method for finishing edit
        //public static List<string> FinalizeEdit(object dataContext, TextBox textBox)
        //{
        //    if (dataContext != null && textBox != null)
        //    {
        //        string text = textBox.Text;
        //        if (!string.IsNullOrWhiteSpace(text))
        //        {
        //            return text.Split(',')
        //                .Select(s => s.Trim())
        //                .Where(s => !string.IsNullOrWhiteSpace(s))
        //                .ToList();
        //        }
        //    }
            
        //    return new List<string>();
        //}
    }
}