using CommandLine;

namespace watchcat
{
    public class Options
    {
        [Option('p', "path", Required = true, HelpText = "List of paths to monitor separated by spaces.")]
        public IEnumerable<string> Paths { get; set; }

        [Option('e', "executable", HelpText = "Path to the executable to execute when changes are detected.")]
        public string Executable { get; set; }

        [Option('a', "args", HelpText = "Arguments to pass to the executable. Everything after this parameter will be passed to the executable.")]
        public string Arguments { get; set; }

        [Option('t', "wait-timeout", Default = 0f, HelpText = "Max time in seconds to wait for program exit. Set to -1 to wait indefinitely. All events will be ignored when waiting.")]
        public float WaitTimeout { get; set; }

        [Option('d', "launch-delay", Default = 0f, HelpText = "Delay in seconds to launch the executable. Change events during the delay restart the timer.")]
        public float LaunchDelay { get; set; }

        [Option('w', "no-window", Default = false, HelpText = "Do not create new window for launched program.")]
        public bool NoWindow { get; set; }

        [Option('u', "load-profile", Default = false, HelpText = "Load user profile for launched program. Only supported on Windows.")]
        public bool LoadProfile { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Verbose output.")]
        public bool Verbose { get; set; }
    }
}
