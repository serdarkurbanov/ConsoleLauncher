using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ConsoleLauncher.Processes
{
    // simplified data for storage
    class ProcessConfig
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public List<string> Arguments { get; set; }
    }

    class FolderConfig
    {
        public string Path { get; set; }
        public List<ProcessConfig> Processes { get; set; }
    }

    static class ProcessSaver
    {
        public static string ConfigFileName = "launcher_config.json";

        public static void Save(FolderContainer container)
        {
            lock (container.Folders)
            {
                var saveData = container.Folders.Select(
                f =>
                {
                    lock (f.Processes)
                    {
                        return new FolderConfig()
                        {
                            Path = f.Path,
                            Processes = f.Processes.Select(
                            p => new ProcessConfig
                            {
                                Name = p.Name,
                                Command = p.Command,
                                Arguments = p.Arguments.ToList()
                            }).ToList()
                        };
                    }
                }).ToList();

                System.IO.File.WriteAllText(ConfigFileName, Newtonsoft.Json.JsonConvert.SerializeObject(saveData));
            }
        }

        public static FolderContainer Restore(System.Windows.Threading.Dispatcher dispatcher)
        {
            FolderContainer result = new FolderContainer(dispatcher);

            try
            {
                List<FolderConfig> data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FolderConfig>>(System.IO.File.ReadAllText(ConfigFileName));

                foreach (var f in data)
                {
                    Processes.Folder folder = new Folder(dispatcher, result);
                    folder.Path = f.Path;

                    foreach (var p in f.Processes)
                    {
                        Processes.Process process = new Process(folder, dispatcher);

                        process.Command = p.Command;
                        process.Name = p.Name;
                        p.Arguments.ForEach(x => process.Arguments.Add(x));

                        folder.Processes.Add(process);
                    }

                    result.Folders.Add(folder);
                }
            }
            catch (System.IO.FileNotFoundException fe)
            {
                MessageBox.Show($"Configuration file not found. Empty folder list will be created. \nException: \n{fe.Message}");
            }
            catch (Newtonsoft.Json.JsonException je)
            {
                MessageBox.Show($"Error while parsing configuration file. Empty folder list will be created. \nException: \n{je.Message}");
            }

            return result;
        }
    }
}
