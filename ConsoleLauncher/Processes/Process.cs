using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

        // dictionary of internal processes and their system assigned names
        object _processTreeSync = new object();
        Dictionary<string, System.Diagnostics.Process> _processTree = new Dictionary<string, System.Diagnostics.Process>();

        // UI elements
        // name displayed
        string _name;
        public string Name { get { return _name; } set { _name = value; _dispatcher.Invoke(() => RaisePropertyChanged("Name")); } }

        string _command;
        public string Command { get { return _command; } set { _command = value; _dispatcher.Invoke(() => RaisePropertyChanged("Command")); } }

        ObservableCollection<string> _arguments = new ObservableCollection<string>();
        public ObservableCollection<string> Arguments { get { return _arguments; } set { _arguments = value; _dispatcher.Invoke(() => RaisePropertyChanged("Arguments")); } }

        // input for the process
        private string _inputString;
        public string InputString { get { return _inputString; } set { _inputString = value; _dispatcher.Invoke(() => RaisePropertyChanged("InputString")); } }

        // all output from the process
        List<OutputRecord> _allRecords = new List<OutputRecord>();
        public List<OutputRecord> AllRecords { get { return _allRecords; } }

        // visible output from the process
        ObservableCollection<OutputRecord> _visibleRecords = new ObservableCollection<OutputRecord>();
        public ObservableCollection<OutputRecord> VisibleRecords { get { return _visibleRecords; } }

        // collection of resource usage
        private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        ObservableCollection<ResourceUsageRecord> _cpuUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> CPUUsage { get { return _cpuUsage; } }


        ObservableCollection<ResourceUsageRecord> _memoryUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> MemoryUsage { get { return _memoryUsage; } }

        ObservableCollection<ResourceUsageRecord> _diskUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> DiskUsage { get { return _diskUsage; } }

        ObservableCollection<ResourceUsageRecord> _threadUsage = new ObservableCollection<ResourceUsageRecord>();
        public ObservableCollection<ResourceUsageRecord> ThreadUsage { get { return _threadUsage; } }

        // peak useage taken from processes
        private double _peakCPUUsage;
        public double PeakCPUUsage { get { return _peakCPUUsage; } set { if (value == 0 || _peakCPUUsage != value) { _peakCPUUsage = value; RaisePropertyChanged("PeakCPUUsage"); } } }

        private double _peakMemoryUsage;
        public double PeakMemoryUsage { get { return _peakMemoryUsage; } set { if (_peakMemoryUsage != value) { _peakMemoryUsage = value; RaisePropertyChanged("PeakMemoryUsage"); } } }

        private double _peakDiskUsage;
        public double PeakDiskUsage { get { return _peakDiskUsage; } set { if (_peakDiskUsage != value) { _peakDiskUsage = value; RaisePropertyChanged("PeakDiskUsage"); } } }

        private double _peakThreadUsage;
        public double PeakThreadUsage { get { return _peakThreadUsage; } set { if (_peakThreadUsage != value) { _peakThreadUsage = value; RaisePropertyChanged("PeakThreadUsage"); } } }

        // process status
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
                DateTime t = new DateTime();
                lock (_internalProcessSync)
                {
                    try
                    {
                        if (_internalProcess != null)
                            t = _internalProcess.StartTime;
                    }
                    catch (Exception e)
                    {
                        // internal process is dead -> do nothing
                    }
                }

                return t;
            }
        }

        // collecting data into record containers
        private void CollectAllRecords_Info(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            OutputRecord r = GetRecord(e, RecordType.Info);
            lock (_allRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    AllRecords.Add(r);
                });
            }
        }
        private void CollectAllRecords_Error(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            OutputRecord r = GetRecord(e, RecordType.Error);
            lock (_allRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    AllRecords.Add(r);
                });
            }
        }
        private void CollectVisibleRecords_Info(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            OutputRecord r = GetRecord(e, RecordType.Info);
            lock (_visibleRecords)
            {
                _dispatcher.Invoke(() =>
                {
                    VisibleRecords.Add(r);
                });
            }
        }
        private void CollectVisibleRecords_Error(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            OutputRecord r = GetRecord(e, RecordType.Error);
            lock (_visibleRecords)
            {
                _dispatcher.Invoke(() =>
                {
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
                    try
                    {
                        r.TimeProcessStart = _internalProcess.StartTime;
                        r.TotalProcessorTime = _internalProcess.TotalProcessorTime;
                        r.ProcessVirtualMemory = _internalProcess.WorkingSet64;
                        r.ProcessThreadCount = _internalProcess.Threads.Count;
                    }
                    catch (Exception)
                    {
                        // something happened with internal process -> do nothing
                    }
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
                            _lastTotalProcessorTime = TimeSpan.Zero;
                            CPUUsage.Clear();
                            MemoryUsage.Clear();
                            DiskUsage.Clear();
                            ThreadUsage.Clear();
                            PeakCPUUsage = 0;
                            PeakDiskUsage = 0;
                            PeakMemoryUsage = 0;
                            PeakThreadUsage = 0;
                        });

                        // start process async
                        _internalProcess = new System.Diagnostics.Process();

                        _internalProcess.StartInfo.FileName = "cmd.exe";
                        _internalProcess.StartInfo.Arguments = "/c " + Command + Arguments.Aggregate("", (x, y) => string.Concat(x, " ", y));
                        _internalProcess.StartInfo.UseShellExecute = false;
                        _internalProcess.StartInfo.CreateNoWindow = true;
                        _internalProcess.StartInfo.RedirectStandardInput = true;
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
                        lock (_processTreeSync)
                        {
                            _processTree.Clear();
                        }
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
            foreach (var p in GetProcessList(pid))
            {
                try
                {
                    p.Kill();
                }
                catch (Exception)
                {
                    // Process already exited.
                }
            }
        }

        // get all processes for given process
        private static List<System.Diagnostics.Process> GetProcessList(int pid)
        {
            List<System.Diagnostics.Process> list = new List<System.Diagnostics.Process>();

            _GetProcesses(pid, list);

            return list;
        }
        private static void _GetProcesses(int pid, List<System.Diagnostics.Process> list)
        {
            try
            {
                var proc = System.Diagnostics.Process.GetProcessById(pid);
                list.Add(proc);
            }
            catch (Exception e)
            {
                // Process already exited -> do nothing
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();


            foreach (ManagementObject mo in moc)
            {
                _GetProcesses(Convert.ToInt32(mo["ProcessID"]), list);
            }
        }

        // get all processes for given process
        private static Dictionary<string, System.Diagnostics.Process> GetProcessDict(int pid)
        {
            Dictionary<string, System.Diagnostics.Process> dict = new Dictionary<string, System.Diagnostics.Process>();

            _GetProcesses(pid, dict);

            return dict;
        }
        private static void _GetProcesses(int pid, Dictionary<string, System.Diagnostics.Process> dict)
        {
            try
            {
                var proc = System.Diagnostics.Process.GetProcessById(pid);
                dict.Add(GetProcessInstanceName(pid), proc);
            }
            catch (Exception e)
            {
                // Process already exited -> do nothing
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();


            foreach (ManagementObject mo in moc)
            {
                _GetProcesses(Convert.ToInt32(mo["ProcessID"]), dict);
            }
        }

        // get process instance name by id
        // taken from http://stackoverflow.com/questions/9115436/performance-counter-by-process-id-instead-of-name
        private static string GetProcessInstanceName(int pid)
        {
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");

            string[] instances = cat.GetInstanceNames();
            foreach (string instance in instances)
            {
                using (PerformanceCounter cnt = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    int val = (int)cnt.RawValue;
                    if (val == pid)
                    {
                        return instance;
                    }
                }
            }
            throw new Exception("couldn't find the process instance name by id");
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

        public ICommand InputCommand
        {
            get
            {
                return new UIHelpers.GenericCommand(
                    (obj) => 
                    {
                        // send input string to the internal process input stream
                        lock (_internalProcessSync)
                        {
                            if (_internalProcess == null)
                                return;

                            try
                            {
                                _internalProcess.StandardInput.WriteLine(InputString);
                                _internalProcess.StandardInput.Flush();
                                InputString = null;
                            }
                            catch (Exception)
                            {
                                // internal process died -> do nothing
                            }
                        }
                    }, 
                    (obj) =>
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
                    lock(_processTreeSync)
                    {
                        _processTree.Clear();
                    }
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

            ResourceUsageRecord cpu = null;
            ResourceUsageRecord mem = null;
            ResourceUsageRecord disk = null;
            ResourceUsageRecord threads = null;

            double cpuCurr = 0;
            double memCurr = 0;
            double diskCurr = 0;
            double threadsCurr = 0;

            try
            {
                lock (_internalProcessSync)
                {
                    lock (_processTreeSync)
                    {
                        // init process tree if needed
                        bool needsInit = false;
                        try
                        {
                            needsInit = _processTree.Count == 0 || _processTree.Any(x => x.Value.HasExited);
                        }
                        catch (Exception)
                        {
                            needsInit = true;
                        }

                        if (needsInit && _internalProcess != null)
                            _processTree = GetProcessDict(_internalProcess.Id);

                        var prevProcessorTimeSpan = _lastTotalProcessorTime;
                        _lastTotalProcessorTime = _processTree.Sum(x => x.Value.TotalProcessorTime);
                        cpuCurr = _lastTotalProcessorTime.Subtract(prevProcessorTimeSpan).TotalMilliseconds / (Environment.ProcessorCount * 1000) * 100;
                        // if list of processes has changed -> total processor time at this stage can be < 0 => do the calculation next time
                        if (cpuCurr >= 0)
                        {
                            cpu = new ResourceUsageRecord(cpuCurr);
                        }

                        memCurr = _processTree.Sum(x => Convert.ToDouble(x.Value.WorkingSet64));
                        mem = new ResourceUsageRecord(memCurr);

                        diskCurr = _processTree.Sum(x => Convert.ToDouble(100));
                        disk = new ResourceUsageRecord(diskCurr);

                        threadsCurr = _processTree.Sum(x => Convert.ToDouble(x.Value.Threads.Count));
                        threads = new ResourceUsageRecord(threadsCurr);
                    }
                }
            }
            catch (Exception)
            {
                // something happened to the process or internal processes => ignore it
            }

            _dispatcher.Invoke(() =>
            {
                if(cpu != null)
                {
                    CPUUsage.Add(cpu);
                    RaisePropertyChanged("CPUUsage");

                    PeakCPUUsage = Math.Max(PeakCPUUsage, cpuCurr);
                }

                if(mem != null)
                {
                    MemoryUsage.Add(mem);
                    RaisePropertyChanged("MemoryUsage");

                    PeakMemoryUsage = Math.Max(PeakMemoryUsage, memCurr);
                }

                if (disk != null)
                {
                    DiskUsage.Add(disk);
                    RaisePropertyChanged("DiskUsage");

                    PeakDiskUsage = Math.Max(PeakDiskUsage, diskCurr);
                }

                if (threads != null)
                {
                    ThreadUsage.Add(threads);
                    RaisePropertyChanged("ThreadUsage");

                    PeakThreadUsage = Math.Max(PeakThreadUsage, threadsCurr);
                }     
            });
        }
    }

    public enum ProcessStatus
    {
        Running,
        Paused,
        Stopped
    }
}
