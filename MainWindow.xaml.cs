using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EPDM.Interop.epdm;

namespace PRS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static IEdmVault7 vault;
        private Thread myThread = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Действия при загрузке программы
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            vault = new EdmVault5();

            IEdmVault8 vault8 = (IEdmVault8)vault;
            EdmViewInfo[] Views = null;
            vault8.GetVaultViews(out Views, false);

            foreach (EdmViewInfo View in Views) { VaultsComboBox.Items.Add(View.mbsVaultName); }
            if (VaultsComboBox.Items.Count > 0)
            { VaultsComboBox.Text = (string)VaultsComboBox.Items[0]; }
        }
        // Действия при выборе Хранилища
        private void VaultsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WorkflowComboBox.Items.Clear();

            vault = new EdmVault5();
            if (!vault.IsLoggedIn) vault.LoginAuto(VaultsComboBox.SelectedItem.ToString(), 0);

            IEdmWorkflowMgr6 wfmgr = (IEdmWorkflowMgr6)vault.CreateUtility(EdmUtility.EdmUtil_WorkflowMgr);
            IEdmPos5 pos = wfmgr.GetFirstWorkflowPosition();

            while (!pos.IsNull)
            {
                IEdmWorkflow6 workflow = wfmgr.GetNextWorkflow(pos);
                WorkflowComboBox.Items.Add(workflow.Name);
            }

            WorkflowComboBox.Text = WorkflowComboBox.Items[0].ToString();
        }
        // Выбор потока работы
        private void WorkflowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StateComboBox.Items.Clear();

            vault = new EdmVault5();
            if (!vault.IsLoggedIn) vault.LoginAuto(VaultsComboBox.SelectedItem.ToString(), 0);

            // Получение выбранного потока работы
            IEdmWorkflowMgr6 wfmgr = (IEdmWorkflowMgr6)vault.CreateUtility(EdmUtility.EdmUtil_WorkflowMgr);
            IEdmPos5 pos = wfmgr.GetFirstWorkflowPosition();
            IEdmWorkflow6 SelectedWorkflow = null;

            while (!pos.IsNull)
            {
                IEdmWorkflow6 workflow = wfmgr.GetNextWorkflow(pos);
                if (workflow.Name == WorkflowComboBox.SelectedItem as string)
                {
                    SelectedWorkflow = workflow;
                    break;
                }
            }

            if (SelectedWorkflow != null)
            {   // Получение состояний выбранного потока работы         
                IEdmPos5 posstate = SelectedWorkflow.GetFirstStatePosition();
                StateComboBox.Items.Add(null);

                while (!posstate.IsNull)
                {
                    IEdmState5 state = SelectedWorkflow.GetNextState(posstate);
                    StateComboBox.Items.Add(state.Name);
                }
            }
        }
        private void StateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //StateComboBox.Items.Add(null);
        }
        // Кнопка поиска
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchLabelText("Идет поиск ...");
            ListView1.Items.Clear();
            myThread = new Thread(new ThreadStart(DoSearch));
            myThread.Start(); // запуск поиска в параллельном потоке
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SearchLabelText("Поиск отменен");
            myThread.Abort();
        }

        private void DoSearch()
        {   // Метод ищет файлы с указанным именем            
            try
            {
                if (vault == null) vault = new EdmVault5();
                if (!vault.IsLoggedIn) vault.LoginAuto(VaultsComboBox.SelectedItem.ToString(), 0);

                string filename = "%" + FileStateBoxText(1) + "%";
                //string filename = "%" + TextBoxFile.Text + "%";
                IEdmSearch6 Search = (IEdmSearch6)vault.CreateSearch();
                Search.FindFolders = false;
                Search.SetToken(EdmSearchToken.Edmstok_FindFiles, true);
                Search.SetToken(EdmSearchToken.Edmstok_Name, filename);
                
                if (FileStateBoxText(2) != null) { Search.SetToken(EdmSearchToken.Edmstok_StateName, FileStateBoxText(2)); }
                if (CheckBoxText(1) == false) { Search.SetToken(EdmSearchToken.Edmstok_Unlocked, false); }
                if (CheckBoxText(2) == false) { Search.SetToken(EdmSearchToken.Edmstok_Locked, false); }

                //if ((string)StateComboBox.SelectedItem != null) { Search.SetToken(EdmSearchToken.Edmstok_StateName, (string)StateComboBox.SelectedItem); }
                //if (CheckBoxReg.IsChecked == false) { Search.SetToken(EdmSearchToken.Edmstok_Unlocked, false); }
                //if (CheckBoxUnreg.IsChecked == false) { Search.SetToken(EdmSearchToken.Edmstok_Locked, false); }

                GetSearchResults(Search, out List<IEdmSearchResult5> Results);

                //SearchLabel.Content = String.Format($"Найдено результатов: {Results.Count}");
                SearchLabelText(String.Format($"Найдено результатов: {Results.Count}"));

                foreach (var result in Results)
                {
                    SelectedFile file = new SelectedFile(result);
                    //ListView1.Items.Add(file);
                    ListView1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView1.Items.Add(file); }));
                }
            }
            catch { }
        }

        private string FileStateBoxText(int i)
        {   // Получает текст из элементов WPF в зависимости от номера
            string text = "";
            switch (i)
            {
                case 1:
                    TextBoxFile.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        try { text = TextBoxFile.Text; }
                        catch { }
                    }));
                    break;
                case 2:
                    StateComboBox.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                    {
                        try { text = StateComboBox.SelectedItem.ToString(); }
                        catch { }
                    }));
                    break;
                default:
                    break;
            }
            return text;
        }

        private void SearchLabelText(string text)
        {   // Передача текста в лейбл с количеством найденных файлов
            SearchLabel.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { SearchLabel.Content = text; }));
        }

        private bool CheckBoxText(int i)
        {   // Получение значений из CheckBox'ов для параметров поиска
            bool val = true;
            switch (i)
            {
                case 1:
                    CheckBoxReg.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { val = (bool)CheckBoxReg.IsChecked; }));
                    break;
                case 2:
                    CheckBoxUnreg.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { val = (bool)CheckBoxUnreg.IsChecked; }));
                    break;
                default:
                    break;
            }
            return val;
        }

        private void GetParents()
        {
            try
            {
                ListView2.Items.Clear();                
                SelectedFile selfile = ListView1.SelectedItem as SelectedFile;
                IEdmFile5 file = vault.GetFileFromPath(selfile.Path, out IEdmFolder5 pFolder);
                IEdmReference7 ParentRefs = file.GetReferenceTree(pFolder.ID) as IEdmReference7;
                if (ParentRefs != null)
                {
                    IEdmPos5 pos = ParentRefs.GetFirstParentPosition2(lVersionOrZero: 0,
                        bGetAllParentVersions: false, lEdmRefFlags: (int)EdmRefFlags.EdmRef_File);

                    while (!pos.IsNull)
                    {  
                        SelectedFile pFile = new SelectedFile(ParentRefs.GetNextParent(pos) as IEdmReference7); 
                        ListView2.Items.Add(pFile);
                    }
                }
            }
            catch { }
        }

        // Действия при выделении найденного элемента
        private void ListView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {   // Метод находит родительские ссылки и заполняет ListView2
            GetParents();
        }

        // Действия при выделении родительских ссылок
        private void ListView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {   // Метод находит возможные переходы состояний для выделенного файла
            try
            {
                ListBoxTransition.Items.Clear();
                SelectedFile file = ListView2.SelectedItem as SelectedFile;
                IEdmState5 fileState = file.File.CurrentState;
                IEdmPos5 pos = fileState.GetFirstTransitionPosition();
                while (!pos.IsNull) { ListBoxTransition.Items.Add(fileState.GetNextTransition(pos).Name); }
                ListBoxTransition.SelectedIndex = 2;
            }
            catch { }            
        }

        void GetSearchResults(IEdmSearch6 Search, out List<IEdmSearchResult5> SearchResults)
        {   // Метод поиска эелментов pdm
            SearchResults = new List<IEdmSearchResult5>();
            IEdmSearchResult5 SearchResult = Search.GetFirstResult();
            while (SearchResult != null)
            {
                SearchResults.Add(SearchResult);
                SearchResult = Search.GetNextResult();
            }
        }
        private void ChangeStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ListView2.SelectedItem != null)
            {
                IEdmBatchChangeState5 BatchChanger = (IEdmBatchChangeState5)vault.CreateUtility(EdmUtility.EdmUtil_BatchChangeState);
                string trans = ListBoxTransition.SelectedItem as string;
                foreach (SelectedFile x in ListView2.SelectedItems)
                {
                    if (x != null) { BatchChanger.AddFile(x.FileID, x.FolderID); }
                }
                
                BatchChanger.CreateTree(trans);
                if (BatchChanger.ShowDlg(0))
                {
                    BatchChanger.ChangeState(0);
                    GetParents();
                }
            }            
        }
        private void ListViewSort(object sender, RoutedEventArgs e)
        {
            List<SelectedFile> list = new List<SelectedFile>();
            foreach (SelectedFile x in ListView1.Items) { list.Add(x); }
            ListView1.Items.Clear();
            var sortedList = list.OrderBy(u => u.Name);
            foreach (SelectedFile x in sortedList) { ListView1.Items.Add(x); }
        }
        private void OpenFolderlv1_Click(object sender, RoutedEventArgs e)
        {
            IEdmVault8 vault8 = (IEdmVault8)vault;
            SelectedFile file = ListView1.SelectedItem as SelectedFile;
            if (file != null) vault8.OpenContainingFolder(file.FileID);
        }
        private void OpenFolderlv2_Click(object sender, RoutedEventArgs e)
        {           
            IEdmVault8 vault8 = (IEdmVault8)vault;
            SelectedFile file = ListView2.SelectedItem as SelectedFile;
            if (file != null) vault8.OpenContainingFolder(file.FileID);
        }
        private void Select_All_lv1_Click(object sender, RoutedEventArgs e)
        {
            ListView1.SelectAll();
        }
        private void Select_All_lv2_Click(object sender, RoutedEventArgs e)
        {
            ListView2.SelectAll();
        }       
    }
}
