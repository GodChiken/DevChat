using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DevChat
{
    class ProjectManager
    {
        public ProjectManager()
        {
            this.ProjectDir = "Projects";
            this.ConfigDir = "Configs";
        }

        private string m_projectDir = "";
        public string ProjectDir
        {
            get { return m_projectDir; }
            set
            {
                m_projectDir = value;
                Directory.CreateDirectory(value);
            }
        }

        private string m_configDir = "";
        public string ConfigDir
        {
            get { return m_configDir; }
            set
            {
                m_configDir = value;
                Directory.CreateDirectory(value);
            }
        }

        public string GetProjectPath(string name)
        {
            return Path.Combine(ProjectDir, name);
        }

        public string GetConfigPath(string name)
        {
            return Path.Combine(ConfigDir, name + ".json");
        }

        public bool Exists(string name)
        {
            return File.Exists(GetConfigPath(name));
        }

        public void ListAllProject(IPushMessage output)
        {
            var buffer = new StringBuilder();

            foreach (string file in Directory.GetFiles(ConfigDir, "*.json"))
            {
                buffer.AppendLine(Path.GetFileNameWithoutExtension(file));
            }

            output.PushMessage(buffer.ToString());
        }

        public void CreateProject(string name, string gitUrl, IPushMessage output,
            IReceiveStreamWriter input)
        {
            if (Exists(name))
            {
                output.PushMessage($"Project {name} already exists.");

                return;
            }


            string configPath = GetConfigPath(name);
            string projPath = GetProjectPath(name);

            var proj = new Project()
            {
                GitUrl = gitUrl,
            };
            SaveProject(name, proj);

            Directory.CreateDirectory(projPath);


            // Clone git repo
            Shell.WorkingDirectory = projPath;
            var process = Shell.Execute("git", $"clone \"{gitUrl}\" .",
                output.PushMessage);

            input.SetStreamWriter(process.StandardInput);

            process.WaitForExit();
            process.Close();
        }

        public void DeleteProject(string name, IPushMessage output)
        {
            if (Exists(name))
            {
                string projPath = GetProjectPath(name);
                string configPath = GetConfigPath(name);

                NoException(() => DeleteDirectory(projPath));
                NoException(() => File.Delete(configPath));
            }
            else
            {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public void InfoProject(string name, IPushMessage output)
        {
            if (Exists(name))
            {
                var proj = LoadProject(name);
                
                output.PushMessage(proj.ToJson());
            }
            else
            {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public void ConfigProject(string name, string prop, string data, IPushMessage output)
        {
            if (Exists(name))
            {
                var proj = LoadProject(name);

                proj.SetProperty(prop, data);

                SaveProject(name, proj);
            }
            else
            {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public void BuildProject(string name, string args, IPushMessage output)
        {
            if (Exists(name))
            {
                var proj = LoadProject(name);


                SyncProject(name, output);


                // Run build script
                if (string.IsNullOrWhiteSpace(proj.BuildScript) == false)
                {
                    var process = Shell.Execute("cmd", "/C " + proj.BuildScript + " " + args,
                        output.PushMessage);

                    process.WaitForExit();
                    process.Close();
                }
            }
            else
            {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public void RunProject(string name, string args, IPushMessage output,
            IReceiveStreamWriter input)
        {
            if (Exists(name))
            {
                var proj = LoadProject(name);


                SyncProject(name, output);


                // Run script
                if (string.IsNullOrWhiteSpace(proj.RunScript) == false)
                {
                    Shell.WorkingDirectory = GetProjectPath(name);
                    var process = Shell.Execute("cmd", "/C " + proj.RunScript + " " + args,
                        output.PushMessage);

                    input.SetStreamWriter(process.StandardInput);

                    process.WaitForExit();
                    process.Close();
                }
            }
            else
            {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public void SyncProject(string name, IPushMessage output)
        {
            if (Exists(name)) {
                // Sync repo
                Shell.WorkingDirectory = GetProjectPath(name);
                string pullResult = Shell.Execute("git", "pull");

                output.PushMessage(pullResult);
            }
            else {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public void CmdProject(string name, string command, IPushMessage output,
            IReceiveStreamWriter input)
        {
            if (Exists(name)) {
                Shell.WorkingDirectory = GetProjectPath(name);
                var process = Shell.Execute("cmd", "/C " + command,
                    output.PushMessage);

                input.SetStreamWriter(process.StandardInput);

                process.WaitForExit();
                process.Close();
            }
            else {
                output.PushMessage($"Project {name} does not exists.");
            }
        }

        public object GetLocker(string name)
        {
            object locker = null;

            lock (m_lockerLocker)
            {
                if (m_projectLocker.ContainsKey(name))
                {
                    locker = m_projectLocker[name];
                }
                else
                {
                    locker = new object();
                    m_projectLocker.Add(name, locker);
                }
            }

            return locker;
        }

        private void NoException(Action action, bool log = false)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (log)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private void DeleteDirectory(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                DeleteDirectory(dir);
            }

            var dirInfo = new DirectoryInfo(path);
            dirInfo.Attributes = FileAttributes.Normal;

            Directory.Delete(path);
        }

        private Project LoadProject(string name)
        {
            var proj = new Project();

            using (var sr = new StreamReader(GetConfigPath(name)))
            {
                var text = sr.ReadToEnd();
                proj.LoadFromJson(text);
            }

            return proj;
        }

        private void SaveProject(string name, Project proj)
        {
            using (var sw = new StreamWriter(GetConfigPath(name)))
            {
                sw.WriteLine(proj.ToJson());
            }
        }

        private readonly object m_lockerLocker = new object();
        private Dictionary<string, object> m_projectLocker = new Dictionary<string, object>();
    }
}
