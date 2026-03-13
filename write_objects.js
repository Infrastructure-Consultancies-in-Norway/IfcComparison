var fs = require("fs");
var dest = "C:/COWIDev/Github/IfcComparison/IfcComparison/Models/IfcComparerObjects.cs";
var lines = [];
lines.push("PLACEHOLDER");
fs.writeFileSync(dest, lines.join("
"), "utf8");
console.log("done");