using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace watchcat
{
    public class Options
    {
        [Value(0, MetaName = "paths", Required = true, HelpText = "List of paths to monitor separated by spaces.")]
        public IEnumerable<string> Paths { get; set; }

        [Option('e', "executable", HelpText = "Path to the executable to execute when changes are detected.")]
        public string Executable { get; set; }

        [Option('a', "args", HelpText = "Arguments to pass to the executable.")]
        public string Arguments { get; set; }

        [Option('t', "wait-timeout", Default = 0f, HelpText = "Max time in seconds to wait for program exit. Set to -1 to wait indefinitely. All events will be ignored when waiting.")]
        public float WaitTimeout { get; set; }

        [Option('d', "launch-delay", Default = 0f, HelpText = "Delay in seconds to launch the executable. Change events during the delay restart the timer.")]
        public float LaunchDelay { get; set; }

        [Option('w', "no-window", Default = false, HelpText = "Do not create new window for launched program.")]
        public bool NoWindow { get; set; }

        [Option('p', "load-profile", Default = false, HelpText = "Load user profile for launched program.")]
        public bool LoadProfile { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Verbose output.")]
        public bool Verbose { get; set; }
    }
}
