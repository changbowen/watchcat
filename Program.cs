using CommandLine;
using System;
using System.Diagnostics;
using System.IO;
using static watchcat.Helpers;

namespace watchcat
{
    internal class Program
    {
        public class Options
        {
            [Value(0, MetaName = "paths", Required = true, HelpText = "List of paths to monitor separated by spaces.")]
            public IEnumerable<string> Paths { get; set; }

            [Option('e', "executable", HelpText = "Path to the executable to execute when changes are detected.")]
            public string Executable { get; set; }

            [Option('a', "args", HelpText = "Arguments to pass to the executable.")]
            public string Arguments { get; set; }

            [Option('t', "timeout", Default = 0, HelpText = "Max time in seconds to wait for program to exit. Set to -1 to wait indefinitely.")]
            public int Timeout { get; set; }
        }

        public static List<FileSystemWatcher> Watchers { get; set; } = new();
        public static Options Opts { get; set; }

        static void Main(string[] args)
        {
            Opts = Parser.Default.ParseArguments<Options>(args).Value;
            if (Opts == null) return;

            AppDomain.CurrentDomain.ProcessExit += OnExit;
            Console.CancelKeyPress += OnExit;

            foreach (var path in Opts.Paths) {
                var di = string.IsNullOrEmpty(path) ? null : new DirectoryInfo(path);
                if (di == null || (int)di.Attributes == -1) {
                    ConsoleWrite($"Path {path} is invalid and will be skipped.", ConsoleColor.Yellow);
                    continue;
                }

                FileSystemWatcher watcher;
                if (di.Attributes.HasFlag(FileAttributes.Directory)) {
                    watcher = new FileSystemWatcher(di.FullName) { IncludeSubdirectories = true };
                }
                else watcher = new FileSystemWatcher(di.Parent.FullName, di.Name);

                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
                watcher.Changed += OnChange;
                watcher.Created += OnChange;
                watcher.Deleted += OnChange;
                watcher.Renamed += OnChange;
                watcher.Error += OnError;
                Watchers.Add(watcher);

                watcher.EnableRaisingEvents = true;
                ConsoleWrite($"Watcher active: {di.FullName}", ConsoleColor.Cyan);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        private static void OnExit<T>(object sender, T e)
        {
            if (Watchers?.Count > 0) {
                foreach (var watcher in Watchers) { watcher.Dispose(); }
                ConsoleWrite("Watchers removed.", ConsoleColor.Cyan);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            ConsoleWrite(e.GetException().ToString(), ConsoleColor.Gray);
        }

        private static void OnChange(object sender, FileSystemEventArgs e)
        {
            ConsoleWrite($"{e.ChangeType}: {e.FullPath}", ConsoleColor.Gray);
            if (string.IsNullOrWhiteSpace(Opts.Executable)) return;

            ConsoleWrite("Starting program...");
            var psi = new ProcessStartInfo() {
                FileName = Opts.Executable,
                Arguments = Opts.Arguments,
                UseShellExecute = false,
                LoadUserProfile = false,
                //CreateNoWindow = true,
            };

            using (var proc = new Process()) {
                if (Opts.Timeout == 0) {
                    proc.StartInfo = psi;
                    proc.Start();
                }
                else {
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    proc.StartInfo = psi;
                    proc.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);

                    proc.Start();
                    proc.BeginOutputReadLine();

                    if (Opts.Timeout > 0)
                        proc.WaitForExit(Opts.Timeout * 1000);
                    else if (Opts.Timeout == -1)
                        proc.WaitForExit();
                }
            }
        }
    }
}