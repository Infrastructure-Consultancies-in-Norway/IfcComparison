using IfcComparison.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcComparison
{
    public interface IUserSettings
    {
        string FilePathOldIFC { get; set; }
        string FilePathNewIFC { get; set; }
        string FilePathIFCToQA { get; set; }
    }
}
