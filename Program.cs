using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
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

            [Option('t', "wait-timeout", Default = 0f, HelpText = "Max time in seconds to wait for program to exit. Set to -1 to wait indefinitely.")]
            public float WaitTimeout { get; set; }

            [Option('d', "launch-delay", Default = 0f, HelpText = "Delay in seconds to launch the executable. Changes during the delay restart the timer.")]
            public float LaunchDelay { get; set; }

            [Option('w', "no-window", Default = false, HelpText = "Do not create new window for started program.")]
            public bool NoWindow { get; set; }

            [Option('p', "load-profile", Default = false, HelpText = "Load user profile for started program.")]
            public bool LoadProfile { get; set; }

            [Option('v', "verbose", Default = false, HelpText = "Verbose output.")]
            public bool Verbose { get; set; }
        }

        public static List<FileSystemWatcher> Watchers { get; set; } = new();
        public static System.Timers.Timer LaunchTimer { get; private set; }
        public static Options Opts { get; set; }

        static void Main(string[] args)
        {
            Opts = Parser.Default.ParseArguments<Options>(args).Value;
            if (Opts == null) return;

            if (Opts.Verbose)
                ConsoleWrite($"Started with options:" +
                    (!string.IsNullOrWhiteSpace(Opts.Executable) ? $"\n  Executable: {Opts.Executable}" : null) +
                    (!string.IsNullOrWhiteSpace(Opts.Arguments) ? $"\n  Arguments: {Opts.Arguments}" : null) +
                    $"\n  Wait for exit: {(Opts.WaitTimeout > 0 ? $"{Opts.WaitTimeout}s" : (Opts.WaitTimeout == 0 ? "Do not wait" : "Indefinitely"))}" +
                    $"\n  Launch delay: {Opts.LaunchDelay}s" +
                    (Opts.NoWindow ? "\n  Do not create new window" : null) +
                    (Opts.LoadProfile ? "\n  Load user profile" : null), ConsoleColor.DarkGray);

            AppDomain.CurrentDomain.ProcessExit += OnExit;
            Console.CancelKeyPress += OnExit;

            if (string.IsNullOrWhiteSpace(Opts.Executable))
                ConsoleWrite("No executable set. No action will be called on changes.", ConsoleColor.Yellow);

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

                watcher.NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.DirectoryName |
                    NotifyFilters.LastWrite;
                watcher.Changed += OnChange;
                watcher.Created += OnChange;
                watcher.Deleted += OnChange;
                watcher.Renamed += OnChange;
                watcher.Error += OnError;
                Watchers.Add(watcher);

                watcher.EnableRaisingEvents = true;
                ConsoleWrite($"Watcher active: {di.FullName}", ConsoleColor.Cyan);
            }

            if (Opts.LaunchDelay > 0) {
                LaunchTimer = new(Opts.LaunchDelay * 1000) { AutoReset = false };
                LaunchTimer.Elapsed += (s, e) => StartProcess();
            }

            ConsoleWrite("Press any key to stop watching...", ConsoleColor.Cyan);
            Console.ReadLine();
        }

        private static void OnExit<T>(object sender, T e)
        {
            if (e is ConsoleCancelEventArgs ec) {
                ec.Cancel = true;
                return;
            }

            if (Watchers?.Count > 0) {
                foreach (var watcher in Watchers) { watcher.Dispose(); }
                LaunchTimer?.Dispose();
                ConsoleWrite("Clean up complete.", ConsoleColor.Cyan);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            ConsoleWrite(e.GetException().ToString(), ConsoleColor.Red);
        }

        private static void OnChange(object sender, FileSystemEventArgs e)
        {
            if (Opts.Verbose)
                ConsoleWrite($"{e.ChangeType} at {DateTime.Now:g}: {e.FullPath}", ConsoleColor.DarkGray);

            if (Opts.LaunchDelay > 0f) {
                LaunchTimer.Stop();
                LaunchTimer.Start();
            }
            else StartProcess();
        }

        private static void StartProcess()
        {
            if (string.IsNullOrWhiteSpace(Opts.Executable)) return;

            if (Opts.Verbose)
                ConsoleWrite($"Launch: {Opts.Executable} {Opts.Arguments}", ConsoleColor.DarkGray);

            Process proc = null;
            try {
                proc = new Process() {
                    StartInfo = new ProcessStartInfo() {
                        FileName = Opts.Executable,
                        Arguments = Opts.Arguments,
                        UseShellExecute = false,
                        LoadUserProfile = Opts.LoadProfile,
                        CreateNoWindow = Opts.NoWindow,
                    }
                };
                proc.Start();

                if (Opts.WaitTimeout > 0)
                    proc.WaitForExit((int)Math.Round(Opts.WaitTimeout * 1000));
                else if (Opts.WaitTimeout == -1)
                    proc.WaitForExit();
            }
            catch (Exception ex) {
                ConsoleWrite($"Error during execution:\n{ex}", ConsoleColor.Red);
            }
            finally {
                proc?.Dispose();
            }
        }
    }
}