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
    public class Process : INotifyPropertyChanged, IDisposable
    {
        private System.Windows.Threading.Dispatcher _dispatcher;
        private Folder _parentFolder;
        public Process(Folder parentFolder, System.Windows.Threading.Dispatcher dispatcher)
        {
            _parentFolder = parentFolder;
            _dispatcher = dispatcher;
        }

        System.Diagnostics.Process _internalProcess;

        // UI elements
        // name displayed
        string _name;
        public string Name { get { return _name; } set { _name = value; _dispatcher.Invoke(() => RaisePropertyChanged("Name")); } }

        string _command;
        public string Command { get { return _command; } set { _command = value; _dispatcher.Invoke(() => RaisePropertyChanged("Command")); } }

        ObservableCollection<string> _arguments = new ObservableCollection<string>();
        public ObservableCollection<string> Arguments { get { return _arguments; } set { _arguments = value; _dispatcher.Invoke(() => RaisePropertyChanged("Arguments")); } }

        // all output from the process
        List<Record> _allRecords = new List<Record>();
        public List<Record> AllRecords { get { return _allRecords; } }

        // visible output from the process
        ObservableCollection<Record> _visibleRecords = new ObservableCollection<Record>();
        public ObservableCollection<Record> VisibleRecords { get { return _visibleRecords; } }

        ProcessStatus _status = ProcessStatus.Stopped;
        public ProcessStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                _dispatcher.Invoke(() =>
                {
                    RaisePropertyChanged("Status");
                    // change accessibility of visual elements if needed
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }


        public DateTime StartTime { get { return _internalProcess == null ? _internalProcess.StartTime : new DateTime(); } }

        // collecting data into record containers
        private void CollectAllRecords_Info(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_allRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    AllRecords.Add(Record.FromDataReceived(e, RecordType.Info));
                });
            }
        }
        private void CollectAllRecords_Error(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_allRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    AllRecords.Add(Record.FromDataReceived(e, RecordType.Error));
                });
            }
        }
        private void CollectVisibleRecords_Info(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_visibleRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    VisibleRecords.Add(Record.FromDataReceived(e, RecordType.Info));
                });
            }
        }
        private void CollectVisibleRecords_Error(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_visibleRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    VisibleRecords.Add(Record.FromDataReceived(e, RecordType.Error));
                });
            }
        }

        // starting/stopping/pausing processes
        private void _StartProcess()
        {
            switch (Status)
            {
                case ProcessStatus.Paused:
                    {
                        // copy data collected since paused
                        lock (_allRecords)
                        {
                            lock (_visibleRecords)
                            {
                                _dispatcher.Invoke(() =>
                                {
                                    foreach (var record in AllRecords.Where(x => x.ID > VisibleRecords.Last().ID))
                                        VisibleRecords.Add(record);
                                });
                            }
                        }

                        // track data again
                        _internalProcess.OutputDataReceived += CollectVisibleRecords_Info;
                        _internalProcess.ErrorDataReceived += CollectVisibleRecords_Error;

                        Status = ProcessStatus.Running;
                    }
                    break;
                case ProcessStatus.Stopped:
                    {
                        // cleanup records
                        _dispatcher.Invoke(() =>
                        {
                            lock (_allRecords)
                            {
                                _allRecords.Clear();
                            }
                            lock (_visibleRecords)
                            {
                                _visibleRecords.Clear();
                            }
                        });

                        // start process async
                        _internalProcess = new System.Diagnostics.Process();

                        _internalProcess.StartInfo.FileName = "cmd.exe";
                        _internalProcess.StartInfo.Arguments = "/c " + Command + Arguments.Aggregate("", (x,y) => string.Concat(x, " ", y));
                        _internalProcess.StartInfo.UseShellExecute = false;
                        _internalProcess.StartInfo.CreateNoWindow = true;
                        _internalProcess.StartInfo.RedirectStandardError = true;
                        _internalProcess.StartInfo.RedirectStandardOutput = true;
                        _internalProcess.StartInfo.WorkingDirectory = _parentFolder.Path;

                        _internalProcess.OutputDataReceived += CollectAllRecords_Info;
                        _internalProcess.ErrorDataReceived += CollectAllRecords_Error;
                        _internalProcess.OutputDataReceived += CollectVisibleRecords_Info;
                        _internalProcess.ErrorDataReceived += CollectVisibleRecords_Error;


                        Task.Run(() =>
                        {
                            try
                            {
                                _internalProcess.Start();
                                _internalProcess.BeginErrorReadLine();
                                _internalProcess.BeginOutputReadLine();
                                _internalProcess.WaitForExit();
                            }
                            catch (Exception e)
                            {
                                // application doesn't start => write about it
                                Record r = new Record() { Content = e.Message, RecordType = RecordType.Error, Time = DateTime.Now };
                                _dispatcher.Invoke(() =>
                                {
                                    VisibleRecords.Add(r);
                                    AllRecords.Add(r);
                                });
                            }
                            finally
                            { 
                                // process ends => set state to stopped
                                _StopProcess();
                            }
                        });

                        Status = ProcessStatus.Running;
                    }
                    break;
                case ProcessStatus.Running:
                    // do nothing
                    break;
                default:
                    throw new Exception($"unknown process status: {Status}");
                    break;
            }
        }
        private void _StopProcess()
        {
            if (_internalProcess != null)
            {
                try
                {
                    _internalProcess.Kill();
                }
                catch
                {
                    // do nothing
                }
                finally
                {
                    _internalProcess.Dispose();
                    _internalProcess = null;
                }
            }

            Status = ProcessStatus.Stopped;
        }
        private void _PauseProcess()
        {
            switch (Status)
            {
                case ProcessStatus.Running:
                    // stop tracking records
                    _internalProcess.OutputDataReceived -= CollectVisibleRecords_Info;
                    _internalProcess.ErrorDataReceived -= CollectVisibleRecords_Error;

                    Status = ProcessStatus.Paused;

                    break;
                case ProcessStatus.Stopped:
                    // process is already stopped => do nothing
                    break;
                case ProcessStatus.Paused:
                    // process is already paused => do nothing
                default:
                    throw new Exception($"unknown process status: {Status}");
                    break;
            }
        }

        // commands
        public ICommand StartCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        _StartProcess();
                    },
                    obj => 
                    {
                        return Status == ProcessStatus.Paused || Status == ProcessStatus.Stopped;
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
                        _StopProcess();
                    },
                    obj =>
                    {
                        return Status == ProcessStatus.Running || Status == ProcessStatus.Paused;
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
                        _PauseProcess();
                    },
                    obj =>
                    {
                        return Status == ProcessStatus.Running;
                    });
            }
        }

        public ICommand EditCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        // open editor
                        UI.ProcessEditor editor = new UI.ProcessEditor();
                        Process p = Process.From(this, editor.Dispatcher);
                        editor.Process = p;

                        if (editor.ShowDialog() ?? false)
                        {
                            // process details were changed => change it in selected process
                            _StopProcess();
                            Command = p.Command;
                            Name = p.Name;
                            Arguments.Clear();
                            foreach (var a in p.Arguments)
                                Arguments.Add(a);

                            // save in config
                            Save();
                        }
                    },
                    obj =>
                    {
                        return true;
                    });
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    obj =>
                    {
                        _parentFolder.Processes.Remove(this);

                        // save in config
                        Save();

                        this.Dispose();
                    },
                    obj =>
                    {
                        return true;
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
            if (_internalProcess != null)
            {
                try
                {
                    _internalProcess.Kill();
                }
                catch
                {
                    // do nothing
                }
                finally
                {
                    _internalProcess.Dispose();
                    _internalProcess = null;
                }
            }

            _disposed = true;
        }
        ~Process()
        {
            if (!_disposed)
                _Dispose(false);
        }

        // saving data
        public void Save()
        {
            _parentFolder.Save();
        }

        // copy from another process
        public static Process From(Process p, System.Windows.Threading.Dispatcher dispatcher)
        {
            Process result = new Process(p._parentFolder, dispatcher)
            { 
                Command = p.Command,
                Name = p.Name
            };
            foreach (var a in p.Arguments)
                result.Arguments.Add(a);

            return result;
        }
    }

    public enum ProcessStatus
    {
        Running,
        Paused,
        Stopped
    }
}
