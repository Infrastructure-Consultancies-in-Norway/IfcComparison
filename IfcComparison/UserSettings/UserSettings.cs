using IfcComparison.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcComparison
{
    public class UserSettings : IUserSettings
    {
        
        public string FilePathOldIFC { get; set; }
        public string FilePathNewIFC { get; set; }
        public string FilePathIFCToQA { get; set; }
        public ObservableCollection<IfcEntity> DataGridContentIFCEntities { get; set ; }


        public UserSettings()
        {

        }

        internal UserSettings(MainViewModel vm)
        {
            FilePathOldIFC = vm.FilePathOldIFC;
            FilePathNewIFC = vm.FilePathNewIFC;
            FilePathIFCToQA = vm.FilePathIFCToQA;
            DataGridContentIFCEntities = vm.DataGridContentIFCEntities;

        }


        /*
        public string FilePathIFC { get; set; }
        public string Discipline { get; set; }
        public string Shortname { get; set; }
        public string FilePathITO { get; set; }
        public string NameITO { get; set; }
        public string FilePathExcelReport { get; set; }
        public string FilePathITOTemplate { get; set; }
        public string XMLFilePath { get; set; }
        public string InitialDir { get; set; }
        public string ExportFolderPath { get; set; }
        public string ExportFileName { get; set; }
        */
    }
}
