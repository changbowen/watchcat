using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using static watchcat.Helpers;

namespace watchcat
{
    internal class Program
    {
        public static List<FileSystemWatcher> Watchers { get; set; } = new();
        public static System.Timers.Timer LaunchTimer { get; private set; }
        public static Options Opts { get; set; }

        static void Main(string[] args)
        {
            var parser = new Parser(settings => {
                settings.GetoptMode = true;
                settings.HelpWriter = Console.Error;
            });

            // get args for exe first
            // all content after -a or --args is stripped from parsing
            var ai = (Array.IndexOf(args, @"-a"), Array.IndexOf(args, @"--args"));
            if (ai.Item1 < 0) ai.Item1 = int.MaxValue;
            if (ai.Item2 < 0) ai.Item2 = int.MaxValue;

            var argIdx = Math.Min(ai.Item1, ai.Item2);
            if (argIdx < int.MaxValue) {
                Opts = parser.ParseArguments<Options>(args[0..argIdx]).Value;
                if (Opts != null) Opts.Arguments = string.Join(' ', args[(argIdx + 1)..]);
            }
            else Opts = parser.ParseArguments<Options>(args).Value;

            if (Opts == null) return;

            if (Opts.Verbose)
                ConsoleWrite($"Started with options:" +
                    (!string.IsNullOrWhiteSpace(Opts.Executable) ? $"\n  Executable: {Opts.Executable}" : null) +
                    (!string.IsNullOrWhiteSpace(Opts.Arguments) ? $"\n  Arguments: {Opts.Arguments}" : null) +
                    $"\n  Wait for exit: {(Opts.WaitTimeout > 0f ? $"{Opts.WaitTimeout}s" : (Opts.WaitTimeout == 0f ? "Do not wait" : "Indefinitely"))}" +
                    $"\n  Launch delay: {Opts.LaunchDelay}s" +
                    (Opts.NoWindow ? "\n  Do not create new window" : null) +
                    (Opts.LoadProfile ? "\n  Load user profile" : null), ConsoleColor.DarkGray);

            AppDomain.CurrentDomain.ProcessExit += OnExit;
            Console.CancelKeyPress += OnExit;

            if (string.IsNullOrWhiteSpace(Opts.Executable))
                ConsoleWrite("No executable set. No action will be called on changes.", ConsoleColor.Yellow);

            foreach (var path in Opts.Paths) {
                FileSystemWatcher watcher;
                if (Directory.Exists(path))
                    watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true };
                else if (File.Exists(path))
                    watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
                else {
                    ConsoleWrite($"Path {path} is invalid and will be skipped.", ConsoleColor.Yellow);
                    continue;
                }

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
                ConsoleWrite($"Watcher active: {path}", ConsoleColor.Cyan);
            }

            if (Watchers.Count == 0) {
                ConsoleWrite($"No watcher is created. Program will exit now.", ConsoleColor.Red);
                return;
            }

            if (Opts.LaunchDelay > 0f) {
                LaunchTimer = new(Opts.LaunchDelay * 1000f) { AutoReset = false };
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
                ConsoleWrite("Watchers removed.", ConsoleColor.Cyan);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            ConsoleWrite(e.GetException().ToString(), ConsoleColor.Red);
        }

        private static void OnChange(object sender, FileSystemEventArgs e)
        {
            if (Opts.Verbose)
                ConsoleWrite($"{e.ChangeType} at {DateTime.Now:s}: {e.FullPath}", ConsoleColor.DarkGray);

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

                if (Opts.WaitTimeout == 0f) return;

                // temporarily disable event when waiting for exit
                if (Opts.Verbose)
                    ConsoleWrite($"Waiting {(Opts.WaitTimeout > 0f ? Opts.WaitTimeout + "s " : null)}for program exit...", ConsoleColor.DarkGray);

                Watchers.ForEach(watcher => { watcher.EnableRaisingEvents = false; });
                if (Opts.WaitTimeout > 0f)
                    proc.WaitForExit((int)Math.Round(Opts.WaitTimeout * 1000));
                else if (Opts.WaitTimeout == -1f)
                    proc.WaitForExit();
                Watchers.ForEach(watcher => { watcher.EnableRaisingEvents = true; });
                
                if (Opts.Verbose)
                    ConsoleWrite($"Watchers resumed.", ConsoleColor.DarkGray);
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