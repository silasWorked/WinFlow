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
        private static int Main(string[] args)
        {
            if (args.Length == 0 || HasArg(args, "-h") || HasArg(args, "--help"))
            {
                PrintUsage();
                return 1;
            }

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
                Console.WriteLine($"WinFlow CLI");
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
                return a;
            }
            return null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: winflow <script.wflow> [--dry-run] [--verbose]");
        }
    }
}
