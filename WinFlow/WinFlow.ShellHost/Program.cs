using System;
using System.IO;
using WinFlow.Core.Model;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Parsing;
using WinFlow.Core.Runtime;

namespace WinFlow.ShellHost
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                return RunInteractiveShell();
            }

            var filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Error: script not found: {filePath}");
                return 2;
            }

            Console.Write($"Run script '{Path.GetFileName(filePath)}'? [Y/n]: ");
            var key = Console.ReadKey(intercept: true);
            Console.WriteLine();
            if (key.Key == ConsoleKey.N)
            {
                Console.WriteLine("Cancelled.");
                return 0;
            }

            var context = new ExecutionContext
            {
                DryRun = false,
                Verbose = false,
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? Environment.CurrentDirectory,
                Log = s => Console.WriteLine(s)
            };

            try
            {
                IParser parser = new WinFlowParser();
                var tasks = parser.Parse(filePath);

                var executor = new TaskExecutor();
                return executor.Run(tasks, context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled error: {ex.Message}");
                return 10;
            }
        }

        private static int RunInteractiveShell()
        {
            var context = new ExecutionContext
            {
                DryRun = false,
                Verbose = false,
                WorkingDirectory = Environment.CurrentDirectory,
                Log = s => Console.WriteLine(s)
            };

            PrintBanner();
            PrintHelp();

            var parser = new WinFlowParser();
            var executor = new TaskExecutor();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("winflow> ");
                Console.ResetColor();
                var line = Console.ReadLine();
                if (line == null) break; // EOF
                line = line.Trim();
                if (line.Length == 0) continue;

                // Meta-commands start with ':' to avoid collision with language
                if (line.StartsWith(":"))
                {
                    var handled = HandleMeta(line, context);
                    if (handled == MetaResult.Exit) break;
                    if (handled == MetaResult.Continue) continue;
                    // if Unknown -> show hint
                    Console.WriteLine("Unknown command. Type :help");
                    continue;
                }

                // Built-in commands without ':' prefix (user-friendly)
                var space = line.IndexOf(' ');
                var cmdName = (space > 0 ? line.Substring(0, space) : line).ToLowerInvariant();
                var cmdArg = space > 0 ? line.Substring(space + 1).Trim() : string.Empty;
                var builtin = HandleBuiltin(cmdName, cmdArg, context);
                if (builtin == MetaResult.Exit) break;
                if (builtin == MetaResult.Continue) continue;

                // Execute a single WinFlow command line by parsing a temporary script
                try
                {
                    var temp = Path.GetTempFileName();
                    File.WriteAllText(temp, line + Environment.NewLine);
                    var tasks = parser.Parse(temp);
                    executor.Run(tasks, context);
                    File.Delete(temp);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }

            return 0;
        }

        private enum MetaResult { Continue, Exit, Unknown }

        private static MetaResult HandleMeta(string line, ExecutionContext context)
        {
            var parts = line.Substring(1).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;
            var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            switch (cmd)
            {
                case "help":
                case "?":
                    PrintHelp();
                    return MetaResult.Continue;
                case "exit":
                case "quit":
                    return MetaResult.Exit;
                case "clear":
                    Console.Clear();
                    return MetaResult.Continue;
                case "pwd":
                    Console.WriteLine(context.WorkingDirectory);
                    return MetaResult.Continue;
                case "cd":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        Console.WriteLine(context.WorkingDirectory);
                        return MetaResult.Continue;
                    }
                    try
                    {
                        var target = Path.IsPathRooted(arg) ? arg : Path.Combine(context.WorkingDirectory, arg);
                        var full = Path.GetFullPath(target);
                        if (!Directory.Exists(full))
                        {
                            Console.WriteLine($"Directory not found: {arg}");
                            return MetaResult.Continue;
                        }
                        context.WorkingDirectory = full;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"cd error: {ex.Message}");
                    }
                    return MetaResult.Continue;
                case "verbose":
                    if (arg.Equals("on", StringComparison.OrdinalIgnoreCase)) context.Verbose = true;
                    else if (arg.Equals("off", StringComparison.OrdinalIgnoreCase)) context.Verbose = false;
                    Console.WriteLine($"verbose={(context.Verbose ? "on" : "off")}");
                    return MetaResult.Continue;
                case "dry":
                case "dry-run":
                    if (arg.Equals("on", StringComparison.OrdinalIgnoreCase)) context.DryRun = true;
                    else if (arg.Equals("off", StringComparison.OrdinalIgnoreCase)) context.DryRun = false;
                    Console.WriteLine($"dry-run={(context.DryRun ? "on" : "off")}");
                    return MetaResult.Continue;
                case "info":
                    PrintInfo();
                    return MetaResult.Continue;
                case "list":
                    PrintAvailableCommands();
                    return MetaResult.Continue;
                case "run":
                case "load":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        Console.WriteLine("Usage: :run <script.wflow>");
                        return MetaResult.Continue;
                    }
                    var path = Path.IsPathRooted(arg) ? arg : Path.Combine(context.WorkingDirectory, arg);
                    if (!File.Exists(path))
                    {
                        Console.WriteLine($"Script not found: {arg}");
                        return MetaResult.Continue;
                    }
                    try
                    {
                        IParser parser = new WinFlowParser();
                        var tasks = parser.Parse(path);
                        var executor = new TaskExecutor();
                        executor.Run(tasks, context);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    return MetaResult.Continue;
                default:
                    return MetaResult.Unknown;
            }
        }

        private static MetaResult HandleBuiltin(string cmd, string arg, ExecutionContext context)
        {
            switch (cmd)
            {
                case "help":
                case "?":
                    PrintHelp();
                    return MetaResult.Continue;
                case "exit":
                case "quit":
                    return MetaResult.Exit;
                case "clear":
                case "cls":
                    Console.Clear();
                    return MetaResult.Continue;
                case "pwd":
                    Console.WriteLine(context.WorkingDirectory);
                    return MetaResult.Continue;
                case "cd":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        Console.WriteLine(context.WorkingDirectory);
                        return MetaResult.Continue;
                    }
                    try
                    {
                        var target = Path.IsPathRooted(arg) ? arg : Path.Combine(context.WorkingDirectory, arg);
                        var full = Path.GetFullPath(target);
                        if (!Directory.Exists(full))
                        {
                            Console.WriteLine($"Directory not found: {arg}");
                            return MetaResult.Continue;
                        }
                        context.WorkingDirectory = full;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"cd error: {ex.Message}");
                    }
                    return MetaResult.Continue;
                case "verbose":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        Console.WriteLine($"verbose={(context.Verbose ? "on" : "off")}");
                        return MetaResult.Continue;
                    }
                    if (arg.Equals("on", StringComparison.OrdinalIgnoreCase)) context.Verbose = true;
                    else if (arg.Equals("off", StringComparison.OrdinalIgnoreCase)) context.Verbose = false;
                    Console.WriteLine($"verbose={(context.Verbose ? "on" : "off")}");
                    return MetaResult.Continue;
                case "dry":
                case "dry-run":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        Console.WriteLine($"dry-run={(context.DryRun ? "on" : "off")}");
                        return MetaResult.Continue;
                    }
                    if (arg.Equals("on", StringComparison.OrdinalIgnoreCase)) context.DryRun = true;
                    else if (arg.Equals("off", StringComparison.OrdinalIgnoreCase)) context.DryRun = false;
                    Console.WriteLine($"dry-run={(context.DryRun ? "on" : "off")}");
                    return MetaResult.Continue;
                case "info":
                    PrintInfo();
                    return MetaResult.Continue;
                case "list":
                    PrintAvailableCommands();
                    return MetaResult.Continue;
                case "run":
                case "load":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        Console.WriteLine("Usage: run <script.wflow>");
                        return MetaResult.Continue;
                    }
                    var path = Path.IsPathRooted(arg) ? arg : Path.Combine(context.WorkingDirectory, arg);
                    if (!File.Exists(path))
                    {
                        Console.WriteLine($"Script not found: {arg}");
                        return MetaResult.Continue;
                    }
                    try
                    {
                        IParser parser = new WinFlowParser();
                        var tasks = parser.Parse(path);
                        var executor = new TaskExecutor();
                        executor.Run(tasks, context);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    return MetaResult.Continue;
                default:
                    return MetaResult.Unknown;
            }
        }

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("WinFlow Shell");
            Console.ResetColor();
            Console.WriteLine("Type :help for help, :exit to quit.");
            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Shell commands:");
            Console.WriteLine("help            Show this help (also :help)");
            Console.WriteLine("exit, quit      Exit shell (also :exit)");
            Console.WriteLine("clear, cls      Clear screen");
            Console.WriteLine("pwd             Print working directory");
            Console.WriteLine("cd <path>       Change working directory");
            Console.WriteLine("verbose on|off  Toggle verbose mode");
            Console.WriteLine("dry on|off      Toggle dry-run mode");
            Console.WriteLine("info            Show system info");
            Console.WriteLine("list            List available commands");
            Console.WriteLine("run <file>      Run a .wflow script");
            Console.WriteLine();
            Console.WriteLine("Execute single-line WinFlow commands directly, e.g.:");
            Console.WriteLine("  echo message=\"Hello\"");
            Console.WriteLine("  file write path=out.txt content=\"data\"");
            Console.WriteLine();
        }

        private static void PrintInfo()
        {
            Console.WriteLine("WinFlow System Information");
            Console.WriteLine("=========================");
            Console.WriteLine($".NET Runtime:  {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"OS:            {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            Console.WriteLine($"Architecture:  {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($"Current Dir:   {Environment.CurrentDirectory}");
            Console.WriteLine($"User:          {Environment.UserName}");
            Console.WriteLine();
        }

        private static void PrintAvailableCommands()
        {
            // Mirror built-ins; could be improved to query dispatcher if exposed
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
        }
    }
}
