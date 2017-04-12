using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot.Wpf;
using System.Collections.ObjectModel;
using OxyPlot;

namespace ConsoleLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            _folderContainer = Processes.ProcessSaver.Restore(Dispatcher);

            PART_ProcessesTreeView.DataContext = _folderContainer;

            _updateResourceTimer = new System.Threading.Timer((obj) => _folderContainer.UpdateResourceRecords(), null, 0, 1000);
        }

        // timer for updating resource usage
        System.Threading.Timer _updateResourceTimer;

        // container for folders
        Processes.FolderContainer _folderContainer;

        // command to copy selected items from the records
        public ICommand CopyCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        StringBuilder sb = new StringBuilder();

                        // selected items should be ordered by id, otherwise the order can be not normal
                        foreach (var r in PART_RecordsListBox.SelectedItems.OfType<Processes.OutputRecord>().OrderBy(x => x.ID))
                            sb.AppendLine(r.Content);

                        Clipboard.Clear();
                        Clipboard.SetText(sb.ToString());
                    },
                    obj => true);
            }
        }

        // kill all processes and dispose all resources when closing
        private void ConsoleManager_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _folderContainer.Dispose();
        }
    }
}
