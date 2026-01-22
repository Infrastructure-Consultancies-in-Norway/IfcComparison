# IFC Entity Display Name Changes

## Summary
Modified the application to display IFC entity names without the leading "I" prefix in the GUI (e.g., "IfcBuildingElementProxy" instead of "IIfcBuildingElementProxy") while maintaining backward compatibility with existing JSON settings files.

## Changes Made

### 1. **IfcTools.cs** - Added Helper Methods
Added two new helper methods to convert between interface names and display names:

- `DisplayNameToInterfaceName(string displayName)`: Converts a display name (e.g., "IfcBuildingElementProxy") to an interface name (e.g., "IIfcBuildingElementProxy")
  - Handles backward compatibility by detecting if the name already has the "IIfc" prefix
  - If input starts with "Ifc" but not "IIfc", adds the leading "I"
  
- `InterfaceNameToDisplayName(string interfaceName)`: Converts an interface name to a display name by removing the leading "I"
  - If input starts with "IIfc", removes the leading "I"
  - Otherwise returns the name as-is

- **Updated `GetInterfaceType(string name)`**: Now accepts both interface names and display names
  - Automatically converts display names to interface names before lookup
  - Ensures all existing code continues to work

### 2. **InterfaceNameConverter.cs** - New WPF Value Converter
Created a new bidirectional converter for XAML data binding:

- **Convert**: Removes leading "I" when displaying in the UI
- **ConvertBack**: Adds leading "I" when saving user input back to the model

### 3. **MainWindow.xaml** - Updated DataGrid Binding
- Added `InterfaceNameConverter` to UserControl resources
- Applied converter to the "IFC Entity" column binding
- Entity names now display without the "I" prefix in the DataGrid

### 4. **SearchWindowViewModel.cs** - Updated Entity List Display
- **`AllIfcEntities()`**: Now uses `InterfaceNameToDisplayName()` to display entity names without "I"
- **`ReturnSelectedItemText()`**: Converts user selection back to interface name using `DisplayNameToInterfaceName()` before storing

### 5. **MainViewModel.cs** - Backward Compatibility for Settings
- **`LoadUserSettings()`**: Normalizes entity names when loading from JSON
  - Calls `DisplayNameToInterfaceName()` on each entity to ensure consistent internal format
  - Handles both old format (with "IIfc") and new format (with "Ifc") in JSON files
  - Internally stores names in interface format (with leading "I")

## Backward Compatibility

The solution maintains full backward compatibility:

1. **Old JSON files** with "IIfcBuildingElementProxy" will continue to work
2. **New JSON files** can use either "IIfcBuildingElementProxy" or "IfcBuildingElementProxy"
3. **Internal storage** always uses the interface format (with "I") for consistency
4. **Display** always shows the user-friendly format (without "I")

### Example JSON File Compatibility
```json
{
  "DataGridContentIFCEntities": [
    {
      "Entity": "IIfcBuildingElementProxy",  // Old format - still works
      ...
    },
    {
      "Entity": "IfcBuildingElementProxy",   // New format - also works
      ...
    }
  ]
}
```

Both formats are automatically normalized to "IIfcBuildingElementProxy" internally when loaded.

## User Experience

### Before:
- Entity names displayed as: `IIfcBuildingElementProxy`, `IIfcWall`, `IIfcColumn`
- Confusing extra "I" at the beginning

### After:
- Entity names displayed as: `IfcBuildingElementProxy`, `IfcWall`, `IfcColumn`
- More intuitive and matches standard IFC naming conventions

## Testing Recommendations

1. Test loading existing JSON settings files with "IIfc" prefix
2. Test manually entering entity names in the DataGrid (with and without "I")
3. Test the search window entity selection
4. Test saving settings and verify JSON format
5. Test the comparison functionality with the new format

## Code Structure

The changes maintain a clean separation:
- **Display Layer** (XAML + Converters): Shows user-friendly names
- **Business Logic** (ViewModels): Handles conversion between formats
- **Data Layer** (Models + IFCTools): Works with interface names internally
