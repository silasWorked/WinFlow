using System;
using System.Collections.Generic;
using System.IO;
using WinFlow.Core.Model;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Parsing;
using WinFlow.Core.Runtime;

namespace WinFlow.Cli
{
    internal static class Program
    {
        private const string Version = "0.1.2";

        private static int Main(string[] args)
        {
            // Handle no arguments -> open a separate shell window
            if (args.Length == 0)
            {
                try
                {
                    LaunchShellWindow();
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to open shell: {ex.Message}");
                    PrintUsage();
                    return 1;
                }
            }

            // Handle version command
            if (HasArg(args, "--version") || HasArg(args, "-v") && args.Length == 1)
            {
                Console.WriteLine($"WinFlow {Version}");
                return 0;
            }

            // Handle help command
            if (HasArg(args, "--help") || HasArg(args, "-h"))
            {
                PrintUsage();
                return 0;
            }

            // Handle shell command (explicit)
            if (HasArg(args, "shell"))
            {
                try
                {
                    LaunchShellWindow();
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to open shell: {ex.Message}");
                    return 1;
                }
            }

            // Handle info command
            if (HasArg(args, "info") || HasArg(args, "--info"))
            {
                PrintInfo();
                return 0;
            }

            // Handle list commands
            if (HasArg(args, "list") || HasArg(args, "--list") || HasArg(args, "commands"))
            {
                PrintAvailableCommands();
                return 0;
            }

            // Script execution mode
            var filePath = GetScriptPath(args);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.Error.WriteLine("Error: script path is required.");
                PrintUsage();
                return 1;
            }

            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Error: script not found: {filePath}");
                return 2;
            }

            var context = new ExecutionContext
            {
                DryRun = HasArg(args, "--dry-run"),
                Verbose = HasArg(args, "--verbose") || HasArg(args, "-v"),
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? Environment.CurrentDirectory,
                Log = s => Console.WriteLine(s)
            };

            if (context.Verbose)
            {
                Console.WriteLine($"WinFlow CLI v{Version}");
                Console.WriteLine($"Script: {Path.GetFullPath(filePath)}");
                Console.WriteLine($"DryRun: {context.DryRun}");
                Console.WriteLine();
            }

            try
            {
                IParser parser = new WinFlowParser();
                var tasks = parser.Parse(filePath);

                var executor = new TaskExecutor();
                var exit = executor.Run(tasks, context);
                return exit;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled error: {ex.Message}");
                if (context.Verbose)
                    Console.Error.WriteLine(ex);
                return 10;
            }
        }

        private static bool HasArg(IReadOnlyList<string> args, string name)
        {
            foreach (var a in args)
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string? GetScriptPath(IReadOnlyList<string> args)
        {
            foreach (var a in args)
            {
                if (a.StartsWith("-")) continue;
                if (a.Equals("info", StringComparison.OrdinalIgnoreCase)) continue;
                if (a.Equals("list", StringComparison.OrdinalIgnoreCase)) continue;
                if (a.Equals("commands", StringComparison.OrdinalIgnoreCase)) continue;
                return a;
            }
            return null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("WinFlow CLI v" + Version);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  winflow                         Open WinFlow shell (new window)");
            Console.WriteLine("  winflow shell                   Open WinFlow shell (new window)");
            Console.WriteLine("  winflow <script.wflow> [OPTIONS]  Run a WinFlow script");
            Console.WriteLine("  winflow --version                 Show version");
            Console.WriteLine("  winflow --help                    Show this help message");
            Console.WriteLine("  winflow info                      Show system information");
            Console.WriteLine("  winflow list                      List available commands");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --dry-run     Simulate execution without making changes");
            Console.WriteLine("  --verbose, -v Show detailed execution logs");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  winflow            # opens new console window");
            Console.WriteLine("  winflow shell      # same as above");
            Console.WriteLine("  winflow demo.wflow");
            Console.WriteLine("  winflow script.wflow --verbose");
            Console.WriteLine("  winflow setup.wflow --dry-run");
        }

        private static void LaunchShellWindow()
        {
            // Try to locate the ShellHost executable
            var shellExe = FindShellHostExecutable();
            if (shellExe == null)
                throw new FileNotFoundException("WinFlow.ShellHost executable not found.");

            // Launch ShellHost in a new console window via 'start'
            var args = $"/c start \"WinFlow Shell\" \"{shellExe}\"";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal,
                WorkingDirectory = Environment.CurrentDirectory
            };
            System.Diagnostics.Process.Start(psi);
        }

        private static string? FindShellHostExecutable()
        {
            // 1. Same directory as CLI (published/installed scenario)
            var baseDir = AppContext.BaseDirectory;
            var candidate1 = Path.Combine(baseDir, "WinFlow.ShellHost.exe");
            if (File.Exists(candidate1)) return Path.GetFullPath(candidate1);

            // 2. Development layout: ..\\..\\.. from net8.0 folder to project root
            try
            {
                var dir = new DirectoryInfo(baseDir);
                // net8.0 -> Debug/Release -> bin -> WinFlow.Cli -> WinFlow (solution folder)
                for (int i = 0; i < 4 && dir.Parent != null; i++) dir = dir.Parent!;
                var root = dir.FullName; // likely ...\\WinFlow
                var debug = Path.Combine(root, "WinFlow.ShellHost", "bin", "Debug", "net8.0", "WinFlow.ShellHost.exe");
                var release = Path.Combine(root, "WinFlow.ShellHost", "bin", "Release", "net8.0", "WinFlow.ShellHost.exe");
                if (File.Exists(debug)) return Path.GetFullPath(debug);
                if (File.Exists(release)) return Path.GetFullPath(release);
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static void PrintInfo()
        {
            Console.WriteLine("WinFlow System Information");
            Console.WriteLine("=========================");
            Console.WriteLine($"Version:       {Version}");
            Console.WriteLine($".NET Runtime:  {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"OS:            {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            Console.WriteLine($"Architecture:  {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($"Current Dir:   {Environment.CurrentDirectory}");
            Console.WriteLine($"User:          {Environment.UserName}");
            Console.WriteLine();
        }

        private static void PrintAvailableCommands()
        {
            Console.WriteLine("WinFlow Built-in Commands");
            Console.WriteLine("=========================");
            Console.WriteLine();
            Console.WriteLine("Core Commands:");
            Console.WriteLine("  echo         Output text messages");
            Console.WriteLine("  noop         No operation (placeholder)");
            Console.WriteLine();
            Console.WriteLine("Environment Module:");
            Console.WriteLine("  env set      Set environment variable");
            Console.WriteLine("  env unset    Remove environment variable");
            Console.WriteLine("  env print    Display environment variables");
            Console.WriteLine();
            Console.WriteLine("File Module:");
            Console.WriteLine("  file write   Create or overwrite a file");
            Console.WriteLine("  file append  Append content to a file");
            Console.WriteLine();
            Console.WriteLine("Process Module:");
            Console.WriteLine("  process.run  Execute process (async, fire-and-forget)");
            Console.WriteLine("  process.exec Execute process (sync, with output capture)");
            Console.WriteLine();
            Console.WriteLine("Example Usage:");
            Console.WriteLine("  echo message=\"Hello World\"");
            Console.WriteLine("  env set name=MY_VAR value=\"test\"");
            Console.WriteLine("  file write path=\"config.txt\" content=\"data\"");
            Console.WriteLine("  process.run file=\"cmd.exe\" args=\"/c dir\"");
            Console.WriteLine();
        }
    }
}
