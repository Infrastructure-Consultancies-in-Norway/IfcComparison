using IfcComparison.Models;
using IfcComparison.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using WpfUtilities.Utils;

namespace IfcComparison
{
    internal class SearchWindowViewModel : NotifierBase
    {
        private string mSelectedItem;
        public string SelectedItem 
        {
            get => mSelectedItem;
            set { SetNotify(ref mSelectedItem, value); } 
        }


        private ObservableCollection<string> mSearchList;
        public ObservableCollection<string> SearchList 
        { 
            get => mSearchList; 
            set => mSearchList = value; 
        }

        private string mSearchText;
        public string SearchText 
        { 
            get => mSearchText;
            set { SetNotify(ref mSearchText, value); FilterSearchList(); } 
        }

        private RelayCommand mSetSelectedItem { get; set; }
        public RelayCommand SetSelectedItem
        {
            get { return mSetSelectedItem ?? (mSetSelectedItem = new RelayCommand(p => { ReturnSelectedItemText(); }, ReturnSelectedItemTextCanUse)); }
        }


        private object CurrentObject { get; set; }

        public SearchWindowViewModel(object currentObject)
        {
            CurrentObject = currentObject;
            mSearchList = AllIfcEntities();
            SearchList = mSearchList;
        }

        private void FilterSearchList()
        {
            var searchList = AllIfcEntities();
            var filteredList = searchList
                .Where(x => x.ToLower().Contains(SearchText))
                .ToList();

            SearchList.Clear();
            foreach (var item in filteredList)
            {
                SearchList.Add(item);
            }

        }

        public static ObservableCollection<string> AllIfcEntities()
        {
            var obsCol = new ObservableCollection<string>();
            foreach (Type type in IfcTools.IfcEntities)
            {
                // Display entity names without the leading "I" for better readability
                string displayName = IfcTools.InterfaceNameToDisplayName(type.Name);
                obsCol.Add(displayName);
            }

            return obsCol;
        }

        public void ReturnSelectedItemText()
        {
            if (SelectedItem != null || CurrentObject != null) 
            {
                var cell = new DataGridCell();
                if (CurrentObject is DataGridCellInfo)
                {
                    var curCell = (DataGridCellInfo)CurrentObject;
                    if (curCell != null)
                    {
                        var cellContent = curCell.Column.GetCellContent(curCell.Item);
                        cell = cellContent.Parent as DataGridCell;
                        if (cell != null)
                        {
                            if (!cell.IsEditing)
                            {
                                cell.IsEditing = true;
                            }
                            var curObj = curCell.Item as IfcEntity;
                            if (curObj == null) { curObj = new IfcEntity(); }
                            if (SelectedItem != null)
                            { 
                                // Convert display name to interface name for internal storage
                                string interfaceName = IfcTools.DisplayNameToInterfaceName(SelectedItem.ToString());
                                curObj.Entity = interfaceName; 
                            }
                            cell.IsEditing = false;
                            cell.Focus();
                        }

                        //var curObj = CurrentObject as DataGridContentIFCEntities;
                        //
                    }
                }

            }
        }
        private bool ReturnSelectedItemTextCanUse(object param)
        {
            return true;
        }




    }


}
