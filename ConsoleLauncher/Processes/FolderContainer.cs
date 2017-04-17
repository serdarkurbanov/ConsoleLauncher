using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConsoleLauncher.Processes
{
    // container for folders
    public class FolderContainer: IDisposable, IUpdateResourceRecords
    {
        Dispatcher _dispatcher;

        public FolderContainer(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        private ObservableCollection<Folder> _folders = new ObservableCollection<Folder>();
        public ObservableCollection<Folder> Folders {  get { return _folders; } }

        // commands
        public ICommand AddFolderCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        Folder f = new Folder(_dispatcher, this);
                        System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog();
                        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            f.Path = d.SelectedPath;

                            lock (Folders)
                            {
                                Folders.Add(f);
                            }
                            // save in config
                            Save();
                        }
                    },
                    obj => { return true; });
            }
        }

        // saving data
        public void Save()
        {
            ProcessSaver.Save(this);
        }


        // dispose resources
        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
                _Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void _Dispose(bool disposing)
        {
            foreach (var f in Folders)
                f.Dispose();

            _disposed = true;
        }

        ~FolderContainer()
        {
            if (!_disposed)
                _Dispose(false);
        }

        // update resource usage on timer
        public void UpdateResourceRecords()
        {
            List<Folder> folders = new List<Folder>();
            lock(Folders)
            {
                folders = Folders.ToList();
            }

            foreach (var f in folders)
                f.UpdateResourceRecords();
        }
    }
}
