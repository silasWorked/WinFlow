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
        private static readonly string HistoryFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".winflow_history");
        
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
            var history = LoadHistory();
            var historyIndex = history.Count;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("winflow> ");
                Console.ResetColor();
                var line = ReadLineWithHistory(history, ref historyIndex);
                if (line == null) break; // EOF
                line = line.Trim();
                if (line.Length == 0) continue;
                
                // Add to history
                if (history.Count == 0 || history[history.Count - 1] != line)
                {
                    history.Add(line);
                    SaveHistory(history);
                }
                historyIndex = history.Count;

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
                    if (!string.IsNullOrWhiteSpace(arg))
                    {
                        var helpText = WinFlow.Core.Runtime.CommandHelp.GetHelp(arg);
                        Console.WriteLine(helpText);
                    }
                    else
                    {
                        PrintHelp();
                    }
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

        private static System.Collections.Generic.List<string> LoadHistory()
        {
            var list = new System.Collections.Generic.List<string>();
            if (File.Exists(HistoryFile))
            {
                try
                {
                    var lines = File.ReadAllLines(HistoryFile);
                    list.AddRange(lines);
                }
                catch { }
            }
            return list;
        }

        private static void SaveHistory(System.Collections.Generic.List<string> history)
        {
            try
            {
                var keep = history.Count > 500 ? history.GetRange(history.Count - 500, 500) : history;
                File.WriteAllLines(HistoryFile, keep);
            }
            catch { }
        }

        private static string? ReadLineWithHistory(System.Collections.Generic.List<string> history, ref int historyIndex)
        {
            var buffer = new System.Text.StringBuilder();
            int cursorPos = 0;
            const int promptLen = 10; // "winflow> " length

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    if (history.Count > 0 && historyIndex > 0)
                    {
                        historyIndex--;
                        ClearLineAndRepaint(buffer, history[historyIndex], ref cursorPos, promptLen);
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex < history.Count - 1)
                    {
                        historyIndex++;
                        ClearLineAndRepaint(buffer, history[historyIndex], ref cursorPos, promptLen);
                    }
                    else if (historyIndex == history.Count - 1)
                    {
                        historyIndex = history.Count;
                        ClearLineAndRepaint(buffer, "", ref cursorPos, promptLen);
                    }
                }
                else if (key.Key == ConsoleKey.Backspace && buffer.Length > 0 && cursorPos > 0)
                {
                    buffer.Remove(cursorPos - 1, 1);
                    cursorPos--;
                    RepaintLine(buffer, cursorPos, promptLen);
                }
                else if (key.Key == ConsoleKey.Delete && cursorPos < buffer.Length)
                {
                    buffer.Remove(cursorPos, 1);
                    RepaintLine(buffer, cursorPos, promptLen);
                }
                else if (key.Key == ConsoleKey.LeftArrow && cursorPos > 0)
                {
                    cursorPos--;
                    SetCursorSafe(promptLen + cursorPos, Console.CursorTop);
                }
                else if (key.Key == ConsoleKey.RightArrow && cursorPos < buffer.Length)
                {
                    cursorPos++;
                    SetCursorSafe(promptLen + cursorPos, Console.CursorTop);
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    cursorPos = 0;
                    SetCursorSafe(promptLen, Console.CursorTop);
                }
                else if (key.Key == ConsoleKey.End)
                {
                    cursorPos = buffer.Length;
                    SetCursorSafe(promptLen + buffer.Length, Console.CursorTop);
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    buffer.Insert(cursorPos, key.KeyChar);
                    cursorPos++;
                    RepaintLine(buffer, cursorPos, promptLen);
                }
                else if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    Console.WriteLine();
                    return null;
                }
            }
        }

        private static void ClearLineAndRepaint(System.Text.StringBuilder buffer, string newContent, ref int cursorPos, int promptLen)
        {
            // Clear from prompt to end of line
            var currentY = Console.CursorTop;
            SetCursorSafe(promptLen, currentY);
            Console.Write(new string(' ', buffer.Length + 10)); // Extra space for safety
            SetCursorSafe(promptLen, currentY);
            
            buffer.Clear();
            buffer.Append(newContent);
            Console.Write(newContent);
            cursorPos = newContent.Length;
        }

        private static void RepaintLine(System.Text.StringBuilder buffer, int cursorPos, int promptLen)
        {
            var currentY = Console.CursorTop;
            SetCursorSafe(promptLen, currentY);
            Console.Write(buffer.ToString() + " ");
            SetCursorSafe(promptLen + cursorPos, currentY);
        }

        private static void SetCursorSafe(int x, int y)
        {
            try
            {
                x = Math.Max(0, x);
                y = Math.Max(0, y);
                if (x < Console.BufferWidth && y < Console.BufferHeight)
                    Console.SetCursorPosition(x, y);
            }
            catch { }
        }

        private static void ClearCurrentLine(int length)
        {
            try
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', length));
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            catch { }
        }

        private static void RedrawLine(System.Text.StringBuilder buffer, int cursorPos)
        {
            try
            {
                var currentX = Console.CursorLeft;
                var currentY = Console.CursorTop;
                var startX = Math.Max(0, currentX - cursorPos);
                
                if (startX >= 0 && startX < Console.BufferWidth)
                {
                    Console.SetCursorPosition(startX, currentY);
                    Console.Write(buffer.ToString() + " ");
                    var newX = startX + cursorPos;
                    if (newX >= 0 && newX < Console.BufferWidth)
                        Console.SetCursorPosition(newX, currentY);
                }
            }
            catch { }
        }
    }
}
