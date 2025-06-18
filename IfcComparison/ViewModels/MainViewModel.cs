using IfcComparison.Enumerations;
using IfcComparison.Models;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfUtilities.Utils;
using Xbim.Ifc;
using Xbim.IO.Xml.BsConf;

namespace IfcComparison.ViewModels
{
    internal class MainViewModel : NotifierBase, IUserSettings
    {

        #region Properties

        private string mFilePathOldIFC;
        public string FilePathOldIFC { get => mFilePathOldIFC; set => SetNotify(ref mFilePathOldIFC, value); }

        private string mFilePathNewIFC;
        public string FilePathNewIFC { get => mFilePathNewIFC; set => SetNotify(ref mFilePathNewIFC, value); }

        private string mFilePathIFCToQA;
        public string FilePathIFCToQA { get => mFilePathIFCToQA; set => SetNotify(ref mFilePathIFCToQA, value); }

        private string mIsOldIFCLoaded;
        public string IsOldIFCLoaded { get => mIsOldIFCLoaded; set => SetNotify(ref mIsOldIFCLoaded, value); }

        private string mIsNewIFCLoaded;
        public string IsNewIFCLoaded { get => mIsNewIFCLoaded; set => SetNotify(ref mIsNewIFCLoaded, value); }

        private string mIsNewIFCQALoaded;
        public string IsNewIFCQALoaded { get => mIsNewIFCQALoaded; set => SetNotify(ref mIsNewIFCQALoaded, value); }

        private string mOutputConsole;
        public string OutputConsole { get => mOutputConsole; set => SetNotify(ref mOutputConsole, value); }

        private string mUserSettingsPath;
        public string UserSettingsPath { get => mUserSettingsPath; set => SetNotify(ref mUserSettingsPath, value); }

        private string mVersion;
        public string Version { get => mVersion; set => SetNotify(ref mVersion, value); }
        public Window SearchWindow { get; set; }

        public IfcStore OldModel { get; set; }
        public IfcStore NewModel { get; set; }
        public IfcStore NewModelQA { get; set; }
        public List<Task<IfcStore>> ModelTaskList { get; set; }

        private DataGridCellInfo mCurrentCell;
        public DataGridCellInfo CurrentCell { get => mCurrentCell; set => SetNotify(ref mCurrentCell, value); }

        private IfcEntity mCurrentItem;
        public IfcEntity CurrentItem { get => mCurrentItem; set => SetNotify(ref mCurrentItem, value); }

        public ObservableCollection<string> ComparisonMethodCol { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<IfcEntity> DataGridContentIFCEntities { get; set; } = new ObservableCollection<IfcEntity>();

        #endregion

        #region Commands

        private ICommand mBrowseOldIFCFileCommand { get; set; }
        public ICommand BrowseOldIFCFileCommand
        {
            get { return mBrowseOldIFCFileCommand ?? (mBrowseOldIFCFileCommand = new CommandHandler(() => GetIFCByName(nameof(BrowseOldIFCFileCommand)), true)); }
        }
        private ICommand mBrowseNewIFCFileCommand { get; set; }
        public ICommand BrowseNewIFCFileCommand
        {
            get { return mBrowseNewIFCFileCommand ?? (mBrowseNewIFCFileCommand = new CommandHandler(() => GetIFCByName(nameof(BrowseNewIFCFileCommand)), true)); }
        }
        private ICommand mBrowseQAIFCFileCommand { get; set; }
        public ICommand BrowseQAIFCFileCommand
        {
            get { return mBrowseQAIFCFileCommand ?? (mBrowseQAIFCFileCommand = new CommandHandler(() => GetIFCByName(nameof(BrowseQAIFCFileCommand)), true)); }
        }

        private ICommand mGenerateIFCPsetCommand { get; set; }
        public ICommand GenerateIFCPsetCommand
        {
            get { return mGenerateIFCPsetCommand ?? (mGenerateIFCPsetCommand = new CommandHandler(() => GenerateIFCPset(), true)); }
        }

        private ICommand mClearOutputCommand { get; set; }
        public ICommand ClearOutputCommand
        {
            get { return mClearOutputCommand ?? (mClearOutputCommand = new CommandHandler(() => ClearOutputText(), true)); }
        }

        private ICommand mSaveUserSettingsCommand { get; set; }
        public ICommand SaveUserSettingsCommand
        {
            get { return mSaveUserSettingsCommand ?? (mSaveUserSettingsCommand = new CommandHandler(() => SaveUserSettings(), true)); }
        }

        private ICommand mLoadUserSettingsCommand { get; set; }
        public ICommand LoadUserSettingsCommand
        {
            get { return mLoadUserSettingsCommand ?? (mLoadUserSettingsCommand = new CommandHandler(() => LoadUserSettings(), true)); }
        }

        private ICommand mLoadModelsCommand { get; set; }
        public ICommand LoadModelsCommand
        {
            get { return mLoadModelsCommand ?? (mLoadModelsCommand = new CommandHandler(() => GetIFCByName(), true)); }
        }

        private ICommand mCopyOutputCommand { get; set; }
        public ICommand CopyOutputCommand
        {
            get;
        }

        private RelayCommand mGetIfcEntityCommand { get; set; }
        public RelayCommand GetIfcEntityCommand
        {
            get { return mGetIfcEntityCommand ?? (mGetIfcEntityCommand = new RelayCommand(p => { GetIfcEntityFromTable(); }, GetIfcEntityFromTableCanUse)); }
        }
        private RelayCommand mBrowseUserSettingsCommand { get; set; }
        public RelayCommand BrowseUserSettingsCommand
        {
            get { return mBrowseUserSettingsCommand ?? (mBrowseUserSettingsCommand = new RelayCommand(p => { GetEntities(); }, p => true)); }
        }

        #endregion

        public MainViewModel()
        {
            var compList = Enum.GetNames(typeof(ComparisonEnumeration))
                .Cast<string>().ToList();
            foreach (var item in compList)
            {
                ComparisonMethodCol.Add(item);
            }

            LoadUserSettings(@"IfcComparisonSettings\\DefaultSettings.json");
            UserSettingsPath = string.Empty;
            ClearOutputText();

            IsOldIFCLoaded = "Not Loaded";
            IsNewIFCLoaded = "Not Loaded";
            IsNewIFCQALoaded = "Not Loaded";

            var versionString = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var appName = Assembly.GetExecutingAssembly().GetName().Name;
            Version = $"{appName} Version: {versionString}";

            GetEntities();
        }

        private void GetEntities()
        {
            var entities = IfcTools.IfcEntities;
        }

        private void SaveUserSettings()
        {
            try
            {
                if (File.Exists(UserSettingsPath))
                {
                    var userSettings = new UserSettings(this);
                    ReadAndWriteJson.WriteAndSerializeAtStartup(userSettings, UserSettingsPath);
                    OutputConsole += $"Settings saved at: {UserSettingsPath}" + Environment.NewLine;
                }
                else
                {
                    Stream stream;
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = "Settings.json";
                    saveFileDialog.Filter = "Json files (*.json)|*.json";
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        UserSettingsPath = saveFileDialog.FileName;
                        stream = saveFileDialog.OpenFile();
                        stream.Close();
                        OutputConsole += $"Setting saved at: {UserSettingsPath}" + Environment.NewLine;
                        ReadAndWriteJson.WriteAndSerializeAtStartup(new UserSettings(this), UserSettingsPath);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserSettings(string userSettingsPath = "")
        {
            if (string.IsNullOrEmpty(userSettingsPath))
            {
                UserSettingsPath = Common.OpenFileDialogPath("json", "");
            }
            else
            {
                UserSettingsPath = userSettingsPath;
            }

            if (!string.IsNullOrEmpty(UserSettingsPath))
            {
                var userSettings = new UserSettings();
                PropertyInfo[] uSetProps;

                userSettings = (UserSettings)ReadAndWriteJson.ReadAndDeserialize<UserSettings>(UserSettingsPath);
                if (userSettings != null)
                {
                    uSetProps = userSettings.GetType().GetProperties();
                    if (uSetProps != null)
                    {
                        foreach (var uSetProp in uSetProps)
                        {
                            var uSetval = uSetProp.GetValue(userSettings);
                            var propName = uSetProp.Name;
                            var vmProp = this.GetType().GetProperty(propName);
                            if (vmProp != null)
                            {
                                if (!(uSetval is ICollection))
                                {
                                    vmProp.SetValue(this, uSetval);
                                }
                                else
                                {
                                    this.DataGridContentIFCEntities.Clear();
                                    foreach (var item in (ICollection<IfcEntity>)uSetval)
                                    {
                                        this.DataGridContentIFCEntities.Add(item);
                                    }
                                }
                            }
                        }
                        OutputConsole += $"Settings loaded from {UserSettingsPath}" + Environment.NewLine;
                    }
                    else
                    {
                        MessageBox.Show("Json file doesn't seem to be valid.");
                        OutputConsole += $"Settings loaded from {UserSettingsPath} failed." + Environment.NewLine;
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Json file doesn't seem to be valid.");
                    OutputConsole += $"Settings loaded from {UserSettingsPath} failed." + Environment.NewLine;
                    return;
                }
            }
        }

        private void ClearOutputText()
        {
            OutputConsole = "";
        }

        private async void GetIFCByName(string commandName)
        {
            var fileName = Common.OpenFileDialogPath("ifc", "");
            var strLoaded = "Loaded";
            var strNotLoaded = "Not Loaded";

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (commandName == nameof(BrowseOldIFCFileCommand))
            {
                IsOldIFCLoaded = strNotLoaded;
                FilePathOldIFC = fileName;
                OutputConsole += $"IFC file: {Path.GetFileName(FilePathOldIFC)} is loading..." + Environment.NewLine;
                OldModel = await OpenIFCModelAsync(FilePathOldIFC);
                IsOldIFCLoaded = strLoaded;
                OutputConsole += $"IFC file: {Path.GetFileName(FilePathOldIFC)} {strLoaded}" + Environment.NewLine;
            }
            else if (commandName == nameof(BrowseNewIFCFileCommand))
            {
                IsNewIFCLoaded = strNotLoaded;
                IsNewIFCQALoaded = strNotLoaded;
                FilePathNewIFC = fileName;
                OutputConsole += $"IFC file: {Path.GetFileName(FilePathNewIFC)} is loading..." + Environment.NewLine;
                if (!string.IsNullOrEmpty(fileName))
                {
                    FilePathIFCToQA = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "_QA" + Path.GetExtension(fileName));
                }
                File.Copy(FilePathNewIFC, FilePathIFCToQA, true);
                OutputConsole += $"IFC file: {Path.GetFileName(FilePathIFCToQA)} is loading..." + Environment.NewLine;

                IsNewIFCLoaded = strLoaded;
                OutputConsole += $"IFC file: {Path.GetFileName(FilePathNewIFC)} {strLoaded}" + Environment.NewLine;

                NewModelQA = await OpenIFCModelAsync(FilePathIFCToQA);
                IsNewIFCQALoaded = strLoaded;
                OutputConsole += $"IFC file: {Path.GetFileName(FilePathIFCToQA)} {strLoaded}" + Environment.NewLine;
            }
        }

        private async Task GetIFCByName()
        {
            var strLoaded = "Loaded";
            var strNotLoaded = "Not Loaded";

            if (string.IsNullOrEmpty(FilePathOldIFC) || string.IsNullOrEmpty(FilePathNewIFC))
            {
                return;
            }

            try
            {
                File.Copy(FilePathNewIFC, FilePathIFCToQA, true);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            IsOldIFCLoaded = strNotLoaded;
            OutputConsole += $"IFC file: {Path.GetFileName(FilePathOldIFC)} is loading..." + Environment.NewLine;

            IsNewIFCLoaded = strNotLoaded;
            OutputConsole += $"IFC file: {Path.GetFileName(FilePathNewIFC)} is loading..." + Environment.NewLine;

            IsNewIFCLoaded = strNotLoaded;
            OutputConsole += $"IFC file: {Path.GetFileName(FilePathIFCToQA)} is loading..." + Environment.NewLine;

            var tasks = new List<Task<IfcStore>>();
            tasks.Add(OpenIFCModelAsync(FilePathOldIFC));
            tasks.Add(OpenIFCModelAsync(FilePathIFCToQA));
            var tasksArr = tasks.ToArray();

            var results = Task.WhenAll(tasksArr);
            await results;

            OldModel = await tasks[0];
            NewModelQA = await tasks[1];

            IsOldIFCLoaded = strLoaded;
            OutputConsole += $"IFC file: {Path.GetFileName(FilePathOldIFC)} {strLoaded}" + Environment.NewLine;

            IsNewIFCLoaded = strLoaded;
            OutputConsole += $"IFC file: {Path.GetFileName(FilePathNewIFC)} {strLoaded}" + Environment.NewLine;

            IsNewIFCQALoaded = strLoaded;
            OutputConsole += $"IFC file: {Path.GetFileName(FilePathIFCToQA)} {strLoaded}" + Environment.NewLine;
        }

        private async void GenerateIFCPset()
        {
            if (File.Exists(FilePathOldIFC) && File.Exists(FilePathNewIFC))
            {
                if (Directory.Exists(Path.GetDirectoryName(FilePathIFCToQA)))
                {
                    if (DataGridContentIFCEntities.Count > 0)
                    {
                        if (IsOldIFCLoaded == "Loaded" && IsNewIFCQALoaded == "Loaded")
                        {
                            OutputConsole += "IFC Comparison running..." + Environment.NewLine;

                            var entitiesList = DataGridContentIFCEntities.ToList();

                            var ifcComparerTask = Models.IfcComparer.CreateAsync(
                                OldModel,
                                NewModelQA,
                                FilePathIFCToQA,
                                "Transaction",
                                entitiesList);

                            var ifcComparer = await ifcComparerTask;

                            await ifcComparer.CompareAllRevisions();

                            OutputConsole += "IFC Comparison completed and written to file." + Environment.NewLine;
                        }
                        else
                        {
                            MessageBox.Show("Models not loaded, wait until labels change to loaded!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("No information set in the IFC Entity settings!");
                    }
                }
                else
                {
                    MessageBox.Show("QA Folder path doesn't exist!");
                }
            }
            else
            {
                MessageBox.Show("Path to IFCs doesn't exist!");
            }
        }

        private async Task<IfcStore> OpenIFCModelAsync(string fileName)
        {
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
            var model = await Task.Run(() => OpenModel(fileName));
            return model;
        }

        private IfcStore OpenModel(string fileName)
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "FKJN",
                ApplicationFullName = "QA BIM",
                ApplicationIdentifier = "QA BIM APP",
                ApplicationVersion = "0.1.0",
                EditorsFamilyName = "Fredrik",
                EditorsGivenName = "Jacobsen",
                EditorsOrganisationName = "COWI AS"
            };

            return IfcStore.Open(fileName, editor, accessMode: Xbim.IO.XbimDBAccess.ReadWrite);
        }

        private void GetIfcEntityFromTable()
        {
            if (SearchWindow == null || !SearchWindow.IsVisible)
            {
                SearchWindow = new SearchWindow(CurrentCell);
            }

            SearchWindow.Focus();
            var cell = new DataGridCell();

            var curCell = CurrentCell;
            var point = new Point();
            if (curCell != null)
            {
                var cellContent = curCell.Column.GetCellContent(curCell.Item);
                if (cellContent != null)
                {
                    cell = cellContent.Parent as DataGridCell;
                    if (cell != null)
                    {
                        point = cell.PointToScreen(point);
                        SearchWindow.Left = point.X;
                        SearchWindow.Top = point.Y + cell.ActualHeight;
                    }
                }
            }
            SearchWindow.Show();
        }

        private bool GetIfcEntityFromTableCanUse(object obj)
        {
            var cell = new DataGridCell();
            var curCell = CurrentCell;
            DataGridColumn col = null;

            if (curCell != null)
            {
                var cellContent = curCell.Column.GetCellContent(curCell.Item);
                if (cellContent != null)
                {
                    cell = cellContent.Parent as DataGridCell;
                    col = cell.Column;
                }
            }

            if ((string)col?.Header == "IFC Entity")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
