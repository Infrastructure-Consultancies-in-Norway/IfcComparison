# IfcComparison

## Sub-project of InfraTools
[InfraTools](https://github.com/Infrastructure-Consultancies-in-Norway/InfraTools/)

### Table of Contents

- [How does it work](#how-does-it-work)
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

### Pro tip!

Use CTRL+F to get the search window to get the IfcEntity. Every entity got an extra letter "I" in front of it.

![](https://i.imgur.com/JR7mD3l.png)

![](https://i.imgur.com/Ek3XXZX.gif)

## Version Updates

Changes are described here:
[CHANGELOG](https://github.com/Infrastructure-Consultancies-in-Norway/InfraTools/blob/master/CHANGELOG.md)