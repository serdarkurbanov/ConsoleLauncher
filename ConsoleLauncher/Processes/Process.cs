using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConsoleLauncher.Processes
{
    public class Process : INotifyPropertyChanged, IDisposable, IUpdateResourceRecords
    {
        private System.Windows.Threading.Dispatcher _dispatcher;
        private Folder _parentFolder;
        public Process(Folder parentFolder, System.Windows.Threading.Dispatcher dispatcher)
        {
            _parentFolder = parentFolder;
            _dispatcher = dispatcher;
        }

        // internal process and sync object
        System.Diagnostics.Process _internalProcess;
        object _internalProcessSync = new object();

        // UI elements
        // name displayed
        string _name;
        public string Name { get { return _name; } set { _name = value; _dispatcher.Invoke(() => RaisePropertyChanged("Name")); } }

        string _command;
        public string Command { get { return _command; } set { _command = value; _dispatcher.Invoke(() => RaisePropertyChanged("Command")); } }

        ObservableCollection<string> _arguments = new ObservableCollection<string>();
        public ObservableCollection<string> Arguments { get { return _arguments; } set { _arguments = value; _dispatcher.Invoke(() => RaisePropertyChanged("Arguments")); } }

        // all output from the process
        List<OutputRecord> _allRecords = new List<OutputRecord>();
        public List<OutputRecord> AllRecords { get { return _allRecords; } }

        // visible output from the process
        ObservableCollection<OutputRecord> _visibleRecords = new ObservableCollection<OutputRecord>();
        public ObservableCollection<OutputRecord> VisibleRecords { get { return _visibleRecords; } }

        // collection of resource usage
        ObservableCollection<ResourceUsageRecord> _cpuUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> CPUUsage { get { return _cpuUsage; } }

        ObservableCollection<ResourceUsageRecord> _memoryUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> MemoryUsage { get { return _memoryUsage; } }

        ObservableCollection<ResourceUsageRecord> _diskUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> DiskUsage { get { return _diskUsage; } }

        ObservableCollection<ResourceUsageRecord> _threadUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> ThreadUsage { get { return _threadUsage; } }

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

        public DateTime StartTime
        {
            get
            {
                lock (_internalProcessSync)
                {
                    return _internalProcess == null ? _internalProcess.StartTime : new DateTime();
                }
            }
        }

        // collecting data into record containers
        private void CollectAllRecords_Info(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_allRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    OutputRecord r = GetRecord(e, RecordType.Info);
                    AllRecords.Add(r);
                });
            }
        }
        private void CollectAllRecords_Error(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_allRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    OutputRecord r = GetRecord(e, RecordType.Error);
                    AllRecords.Add(r);
                });
            }
        }
        private void CollectVisibleRecords_Info(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_visibleRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    OutputRecord r = GetRecord(e, RecordType.Info);
                    VisibleRecords.Add(r);
                });
            }
        }
        private void CollectVisibleRecords_Error(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (_visibleRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    OutputRecord r = GetRecord(e, RecordType.Error);
                    VisibleRecords.Add(r);
                });
            }
        }

        // get record for this output data
        private OutputRecord GetRecord(System.Diagnostics.DataReceivedEventArgs e, RecordType recType)
        {
            OutputRecord r = OutputRecord.FromDataReceived(e, recType);

            lock (_internalProcessSync)
            {
                if (_internalProcess != null)
                {
                    r.TimeProcessStart = _internalProcess.StartTime;
                    r.TotalProcessorTime = _internalProcess.TotalProcessorTime;
                    r.ProcessVirtualMemory = _internalProcess.WorkingSet64;
                    r.ProcessThreadCount = _internalProcess.Threads.Count;
                }
            }

            return r;
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

                        lock (_internalProcessSync)
                        {
                            // track data again
                            _internalProcess.OutputDataReceived += CollectVisibleRecords_Info;
                            _internalProcess.ErrorDataReceived += CollectVisibleRecords_Error;
                        }

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
                            CPUUsage.Clear();
                            MemoryUsage.Clear();
                            DiskUsage.Clear();
                            ThreadUsage.Clear();
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
                                OutputRecord r = new OutputRecord() { Content = e.Message, RecordType = RecordType.Error, Time = DateTime.Now };
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
            lock (_internalProcessSync)
            {
                if (_internalProcess != null)
                {
                    try
                    {
                        _internalProcess.OutputDataReceived -= CollectVisibleRecords_Info;
                        _internalProcess.ErrorDataReceived -= CollectVisibleRecords_Error;
                        _internalProcess.OutputDataReceived -= CollectAllRecords_Info;
                        _internalProcess.ErrorDataReceived -= CollectAllRecords_Error;

                        KillProcess(_internalProcess.Id);
                    }
                    catch
                    {
                        // process already killed -> do nothing
                    }
                    finally
                    {
                        _internalProcess.Dispose();
                        _internalProcess = null;
                    }
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
                    // process is paused => start tracking it again
                    _StartProcess();
                    break;
                default:
                    throw new Exception($"unknown process status: {Status}");
                    break;
            }
        }

        // kill child processes
        private static void KillProcess(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcess(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                var proc = System.Diagnostics.Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
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
                        lock (_parentFolder.Processes)
                        {
                            _parentFolder.Processes.Remove(this);
                        }
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

        // update resource usage on timer
        public void UpdateResourceRecords()
        {
            if (Status == ProcessStatus.Stopped)
                return;

            lock (_internalProcessSync)
            {
                if (_internalProcess != null)
                {
                    _dispatcher.Invoke(() =>
                    {
                        try
                        {
                            CPUUsage.Add(new ResourceUsageRecord(20));
                            MemoryUsage.Add(new ResourceUsageRecord(Convert.ToDouble(_internalProcess.WorkingSet64)));
                            DiskUsage.Add(new ResourceUsageRecord(Convert.ToDouble(100)));
                            ThreadUsage.Add(new ResourceUsageRecord(Convert.ToDouble(_internalProcess.Threads.Count)));
                        }
                        catch (InvalidOperationException ie)
                        {
                            // do nothing
                        }
                        catch (Exception e)
                        {
                            // do nothing
                        }
                    });
                }
            }
        }
    }

    public enum ProcessStatus
    {
        Running,
        Paused,
        Stopped
    }
}
