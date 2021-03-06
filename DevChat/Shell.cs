using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace DevChat
{
    static class Shell
    {
        public static string WorkingDirectory { get; set; }

        public static Process Execute(string file, string argument, Action<string> outputHandler)
        {
            var info = new ProcessStartInfo(file)
            {
                WorkingDirectory = WorkingDirectory,
                Arguments = argument,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            var proc = new FixedProcess();
            proc.StartInfo = info;
            proc.EnableRaisingEvents = true;

            proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.Write(e.Data);

                    outputHandler(e.Data);
                }
            });
            proc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.Write(e.Data);

                    outputHandler(e.Data);
                }
            });

            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            return proc;
        }

        public static string Execute(string file, string argument)
        {
            var output = new StringBuilder();
            object locker = new object();

            var proc = Execute(file, argument, data =>
            {
                lock (locker)
                {
                    output.Append(data);
                }
            });

            proc.WaitForExit();
            proc.Close();

            return output.ToString();
        }
    }
}
