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
                Console.WriteLine("WinFlow ShellHost");
                Console.WriteLine("Usage: (double-click .wflow associated) or: ShellHost <script.wflow>");
                return 1;
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
    }
}
