# Watch Cat

Command line tool to watch for changes in directories / files and perform actions when changes are detected.

Available options:

```
-p, --path            Required. List of paths to monitor separated by spaces.
-e, --executable      Path to the executable to execute when changes are detected.
-a, --args            Arguments to pass to the executable. Everything after this parameter will be passed to the executable.
-t, --wait-timeout    (Default: 0) Max time in seconds to wait for program exit. Set to -1 to wait indefinitely. All events will be ignored when waiting.
-d, --launch-delay    (Default: 0) Delay in seconds to launch the executable. Change events during the delay restart the timer.
-w, --no-window       (Default: false) Do not create new window for launched program.
-u, --load-profile    (Default: false) Load user profile for launched program. Only supported on Windows.
-v, --verbose         (Default: false) Verbose output.
--help                Display this help screen.
--version             Display version information.
```

Examples:

```
watchcat --path C:\doc\docs `
         --path C:\doc\mkdocs.yml `
         --launch-delay 20 `
         --executable python.exe `
         --args -m mkdocs build -f mkdocs.yml
```