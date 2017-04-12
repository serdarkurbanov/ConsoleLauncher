using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConsoleLauncher.Processes
{
    // viewmodel for folder
    public class Folder : INotifyPropertyChanged, IDisposable, IUpdateResourceRecords
    {
        // dispatcher of the window
        private System.Windows.Threading.Dispatcher _dispatcher;
        // parent container
        private FolderContainer _parentContainer;
        public Folder(System.Windows.Threading.Dispatcher dispatcher, FolderContainer parentContainer)
        {
            _dispatcher = dispatcher;
            _parentContainer = parentContainer;
        }

        // UI elements
        string _path;
        public string Path {  get { return _path; } set { _path = value; _dispatcher.Invoke(() => RaisePropertyChanged("Path")); } }

        ObservableCollection<Process> _processes = new ObservableCollection<Process>();
        public ObservableCollection<Process> Processes { get { return _processes; } }

        // commands
        public ICommand AddProcessCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj => 
                    {
                        // open process editor
                        UI.ProcessEditor editor = new UI.ProcessEditor();
                        Process p = new Process(this, editor.Dispatcher);
                        editor.Process = p;

                        if (editor.ShowDialog() ?? false)
                        {
                            // new process successfully added => add it to the list
                            Process viewProcess = Process.From(p, _dispatcher);

                            lock (Processes)
                            {
                                Processes.Add(viewProcess);
                            }
                            // save in config
                            Save();
                            
                        }
                    }, 
                    obj => { return true; });
            }
        }
        public ICommand EditFolderCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog();
                        d.SelectedPath = Path;
                        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            Path = d.SelectedPath;

                            // save in config
                            Save();
                        }
                    },
                    obj => { return true; });
            }
        }
        public ICommand DeleteFolderCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        lock (_parentContainer.Folders)
                        {
                            _parentContainer.Folders.Remove(this);
                        }
                        // save in config
                        Save();
                        

                        this.Dispose();
                    },
                    obj => { return true; });
            }
        }

        // start/stop commands for all processes
        public ICommand StartCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        lock (Processes)
                        {
                            // start all processes that can be started
                            foreach (var p in Processes.Where(x => x.StartCommand.CanExecute(obj)))
                            {
                                p.StartCommand.Execute(obj);
                            }
                        }
                    },
                    obj =>
                    {
                        lock (Processes)
                        {
                            // true if any of the processes can be started
                            return Processes.Count(x => x.StartCommand.CanExecute(obj)) > 0;
                        }
                    });
            }
        }
        public ICommand StopCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        lock (Processes)
                        {
                            // start all processes that can be stopped
                            foreach (var p in Processes.Where(x => x.StopCommand.CanExecute(obj)))
                            {
                                p.StartCommand.Execute(obj);
                            }
                        }
                    },
                    obj =>
                    {
                        lock (Processes)
                        {
                            // true if any of the processes can be stopped
                            return Processes.Count(x => x.StopCommand.CanExecute(obj)) > 0;
                        }
                    });
            }
        }
        public ICommand PauseCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        lock (Processes)
                        {
                            // start all processes that can be paused
                            foreach (var p in Processes.Where(x => x.PauseCommand.CanExecute(obj)))
                            {
                                p.StartCommand.Execute(obj);
                            }
                        }
                    },
                    obj =>
                    {
                        lock (Processes)
                        {
                            // true if any of the processes can be paused
                            return Processes.Count(x => x.PauseCommand.CanExecute(obj)) > 0;
                        }
                    });
            }
        }

        // UI change notification
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        // cleaing internal resources
        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
                _Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void _Dispose(bool disposing)
        {
            foreach(var p in _processes)
                p.Dispose();

            _disposed = true;
        }
        ~Folder()
        {
            if (!_disposed)
                _Dispose(false);
        }

        // saving data
        public void Save()
        {
            _parentContainer.Save();
        }

        // update resource usage on timer
        public void UpdateResourceRecords()
        {
            lock (Processes)
            {
                foreach (var p in Processes)
                {
                    p.UpdateResourceRecords();
                }
            }
        }
    }
}
