# IfcComparison

## Sub-project of COWI Bridge Toolbox
[COWI Bridge Toolbox](http://git.cowiportal.com/Team-Bridges-Norway/COWI_Bridge_Toolbox_App)

### Table of Contents

- [How does it work](#how-does-it-work)
- [CLI Mode](#cli-mode)
- [Version updates](#version-updates)
- [BUGS](#bugs)

## How does it work

1. We've added the possibility to use all Ifc entities from the IFC4 "interfaces" (IFC2x3 files will also work!) specified in the XBim library. This means you can choose all of the entities as listed here:
   https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD2_TC1/HTML/link/alphabeticalorder-entities.htm
   Every discipline should be able to use it.

2. The application will check every property in specified property set and entity. A **caveat** is that it will fetch the first object it finds and dismiss similar property sets later in the loop. This can best be described with an example.

   If you have some reinforcement and use the "contains" functionality on rebar position number and have two or more objects with the same position number you will not get an accumulated count or center distance, only the first object the application find is considered.

3. Geometry changes are not implemented, but it's possible to achieve.

4. There's no automation possibilities at the moment, but this is of course possible.

## CLI Mode

The application can now be run in **CLI (Command Line Interface) mode** for automation and batch processing. This allows you to run IFC comparisons without opening the GUI.

### Usage

Run the application with a JSON settings file:

```bash
# Simple usage
IfcComparison.exe "path\to\settings.json"

# Using --settings flag
IfcComparison.exe --settings "path\to\settings.json"

# Show help
IfcComparison.exe --help
```

### Settings File Format

Create a JSON file with the following structure:

```json
{
  "FilePathOldIFC": "C:\\path\\to\\old_model.ifc",
  "FilePathNewIFC": "C:\\path\\to\\new_model.ifc",
  "FilePathIFCToQA": "C:\\path\\to\\output\\qa_model.ifc",
  "DataGridContentIFCEntities": [
    {
      "PSetName": "QA_REBAR",
      "Entity": "IIfcReinforcingBar",
      "IfcPropertySets": [ "MERKNADER" ],
      "ComparisonOperator": "K200",
      "ComparisonMethod": "Contains"
    },
    {
      "PSetName": "QA_PROXY",
      "Entity": "IIfcBuildingElementProxy",
      "IfcPropertySets": [ "MERKNADER" ],
      "ComparisonOperator": "K001",
      "ComparisonMethod": "Contains"
    }
  ]
}
```

### Required Fields

- **FilePathOldIFC**: Path to the original IFC file
- **FilePathNewIFC**: Path to the new/updated IFC file
- **FilePathIFCToQA**: Path where the output QA IFC file will be saved
- **DataGridContentIFCEntities**: Array of entity configurations for comparison

### Entity Configuration

Each entity in the array must have:

- **PSetName**: Name of the property set to create in the output
- **Entity**: IFC entity type (e.g., "IIfcReinforcingBar", "IIfcBeam")
- **IfcPropertySets**: Array of property set names to check
- **ComparisonOperator**: The value to search for
- **ComparisonMethod**: Comparison method - valid values:
  - `Equals` - Exact match
  - `Contains` - Partial match
  - `StartsWith` - Starts with value
  - `EndsWith` - Ends with value

### CLI Output

The CLI provides detailed output including:
- Configuration validation errors (if any)
- Loading progress for IFC models
- Comparison progress
- Success or error messages with exit codes

**Exit Codes:**
- `0` - Success
- `1` - Error (validation failed, file not found, comparison error, etc.)

### Example Automation

Integrate into batch scripts or CI/CD pipelines:

```batch
@echo off
echo Running IFC Comparison...
IfcComparison.exe "config\comparison_settings.json"
if %ERRORLEVEL% NEQ 0 (
    echo Comparison failed!
    exit /b 1
)
echo Comparison completed successfully!
```

### Pro tip!

Use CTRL+F to get the search window to get the IfcEntity. Every entity got an extra letter "I" in front of it.

![](http://git.cowiportal.com/Team-Bridges-Norway/COWI_Bridge_Toolbox_App/raw/branch/master/Documentation/Screenshot.png)

![](http://git.cowiportal.com/Team-Bridges-Norway/COWI_Bridge_Toolbox_App/raw/branch/master/Documentation/Example.gif)

## Version Updates

Changes are described here:
[CHANGELOG](http://git.cowiportal.com/Team-Bridges-Norway/COWI_Bridge_Toolbox_App/src/branch/master/CHANGELOG.md)