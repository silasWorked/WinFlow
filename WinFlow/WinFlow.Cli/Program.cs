using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WinFlow.Core.Model;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Parsing;
using WinFlow.Core.Runtime;

namespace WinFlow.Cli
{
    internal static class Program
    {
        private const string Version = "0.2.0";

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

            // Handle update commands
            if (HasArg(args, "check-update") || HasArg(args, "--check-update"))
            {
                return CheckUpdate().GetAwaiter().GetResult();
            }
            if (HasArg(args, "update") || HasArg(args, "self-update") || HasArg(args, "upgrade"))
            {
                return PerformUpdate().GetAwaiter().GetResult();
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

            // Handle --url flag (download and run script)
            var urlArg = GetArgValue(args, "--url");
            if (!string.IsNullOrWhiteSpace(urlArg))
            {
                try
                {
                    var tempFile = Path.Combine(Path.GetTempPath(), "winflow_" + Guid.NewGuid() + ".wflow");
                    using var http = new HttpClient();
                    var data = http.GetByteArrayAsync(urlArg).GetAwaiter().GetResult();
                    File.WriteAllBytes(tempFile, data);
                    
                    var urlContext = new ExecutionContext
                    {
                        DryRun = HasArg(args, "--dry-run"),
                        Verbose = HasArg(args, "--verbose") || HasArg(args, "-v"),
                        WorkingDirectory = Environment.CurrentDirectory,
                        Log = s => Console.WriteLine(s),
                        LogError = s => 
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine(s);
                            Console.ResetColor();
                        },
                        LogWarning = s =>
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(s);
                            Console.ResetColor();
                        }
                    };

                    if (urlContext.Verbose)
                    {
                        Console.WriteLine($"WinFlow CLI v{Version}");
                        Console.WriteLine($"Downloaded from: {urlArg}");
                        Console.WriteLine($"DryRun: {urlContext.DryRun}");
                        Console.WriteLine();
                    }

                    IParser parser = new WinFlowParser();
                    var tasks = parser.Parse(tempFile);
                    var executor = new TaskExecutor();
                    var result = executor.Run(tasks, urlContext);
                    File.Delete(tempFile);
                    return result;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: Failed to download/execute from URL: {ex.Message}");
                    return 1;
                }
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
                Log = s => Console.WriteLine(s),
                LogError = s => 
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(s);
                    Console.ResetColor();
                },
                LogWarning = s =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(s);
                    Console.ResetColor();
                }
            };

            // Handle --log-file argument
            var logFile = GetArgValue(args, "--log-file");
            if (!string.IsNullOrWhiteSpace(logFile))
            {
                context.LogFile = logFile;
                var originalLog = context.Log;
                context.Log = s =>
                {
                    originalLog(s);
                    try { System.IO.File.AppendAllText(logFile, s + Environment.NewLine); } catch { }
                };
            }

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

        private static string? GetArgValue(IReadOnlyList<string> args, string name)
        {
            for (int i = 0; i < args.Count - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
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
            Console.WriteLine("  winflow --url <URL> [OPTIONS]   Download and run script from URL");
            Console.WriteLine("  winflow --version                 Show version");
            Console.WriteLine("  winflow --help                    Show this help message");
            Console.WriteLine("  winflow info                      Show system information");
            Console.WriteLine("  winflow list                      List available commands");
            Console.WriteLine("  winflow check-update              Check for a newer version");
            Console.WriteLine("  winflow update                    Download and install the latest version");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --dry-run          Simulate without executing");
            Console.WriteLine("  --verbose, -v      Enable verbose output");
            Console.WriteLine("  --log-file <path>  Write output to log file");
            Console.WriteLine("  --url <URL>        Download and execute script from URL");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  winflow            # opens new console window");
            Console.WriteLine("  winflow shell      # same as above");
            Console.WriteLine("  winflow demo.wflow");
            Console.WriteLine("  winflow script.wflow --verbose");
            Console.WriteLine("  winflow setup.wflow --dry-run");
            Console.WriteLine("  winflow --url https://example.com/script.wflow");
            Console.WriteLine("  winflow check-update");
            Console.WriteLine("  winflow update");
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

        // --- Self-update ---
        private static async Task<int> CheckUpdate()
        {
            try
            {
                var latest = await GetLatestReleaseAsync();
                if (latest == null)
                {
                    Console.WriteLine("No release info available.");
                    return 0;
                }
                var currentV = ParseVersion(Version);
                var latestV = ParseVersion(latest.Tag);
                if (latestV == null || currentV == null)
                {
                    Console.WriteLine($"Current: {Version}, Latest tag: {latest.Tag}");
                    return 0;
                }
                if (CompareVersions(latestV, currentV) > 0)
                {
                    Console.WriteLine($"Update available: {currentV} -> {latestV}");
                    Console.WriteLine($"Asset: {latest.AssetName}");
                }
                else
                {
                    Console.WriteLine($"You are up to date ({currentV}). Latest: {latestV}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"check-update failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> PerformUpdate()
        {
            try
            {
                var latest = await GetLatestReleaseAsync();
                if (latest == null)
                {
                    Console.Error.WriteLine("Could not fetch latest release.");
                    return 1;
                }

                var currentV = ParseVersion(Version);
                var latestV = ParseVersion(latest.Tag);
                if (latestV != null && currentV != null && CompareVersions(latestV, currentV) <= 0)
                {
                    Console.WriteLine("Already up to date.");
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(latest.DownloadUrl))
                {
                    Console.Error.WriteLine("No downloadable asset found in the latest release.");
                    return 1;
                }

                var tempZip = Path.Combine(Path.GetTempPath(), $"winflow-update-{Guid.NewGuid():N}.zip");
                var tempDir = Path.Combine(Path.GetTempPath(), $"winflow-update-{Guid.NewGuid():N}");

                Console.WriteLine($"Downloading: {latest.AssetName}");
                using (var http = CreateHttpClient())
                {
                    var data = await http.GetByteArrayAsync(latest.DownloadUrl);
                    await File.WriteAllBytesAsync(tempZip, data);
                }

                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(tempZip, tempDir, overwriteFiles: true);

                // Find installer inside extracted package
                var installerPath = Path.Combine(tempDir, "WinFlow.Installer.Cli.exe");
                if (!File.Exists(installerPath))
                {
                    // fallback: search recursively
                    var found = Directory.GetFiles(tempDir, "WinFlow.Installer.Cli.exe", SearchOption.AllDirectories);
                    if (found.Length > 0) installerPath = found[0];
                }
                if (!File.Exists(installerPath))
                {
                    Console.Error.WriteLine("Installer not found in the downloaded package.");
                    return 1;
                }

                var installDir = GetDefaultInstallDir();
                Console.WriteLine($"Starting installer to update: {installDir}");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = $"--dir \"{installDir}\"",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(installerPath) ?? tempDir,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
                };
                System.Diagnostics.Process.Start(psi);

                Console.WriteLine("Updater started. This process will exit now to allow file replacement.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"update failed: {ex.Message}");
                return 1;
            }
        }

        private static string GetDefaultInstallDir()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinFlow");
            return dir;
        }

        private static HttpClient CreateHttpClient()
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("WinFlowCLI/" + Version);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return http;
        }

        private static global::System.Version? ParseVersion(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
            if (global::System.Version.TryParse(s, out var v)) return v;
            return null;
        }

        private static int CompareVersions(global::System.Version a, global::System.Version b)
        {
            return a.CompareTo(b);
        }

        private sealed class ReleaseInfo
        {
            public string Tag { get; set; } = "";
            public string AssetName { get; set; } = "";
            public string DownloadUrl { get; set; } = "";
        }

        private static async Task<ReleaseInfo?> GetLatestReleaseAsync()
        {
            var api = "https://api.github.com/repos/silasWorked/WinFlow/releases/latest";
            using var http = CreateHttpClient();
            using var resp = await http.GetAsync(api);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            string assetName = "";
            string downloadUrl = "";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.GetProperty("name").GetString() ?? "";
                    var url = a.GetProperty("browser_download_url").GetString() ?? "";
                    if (name.Equals("winflow-win-x64.zip", StringComparison.OrdinalIgnoreCase))
                    {
                        assetName = name;
                        downloadUrl = url;
                        break;
                    }
                }
            }
            return new ReleaseInfo { Tag = tag, AssetName = assetName, DownloadUrl = downloadUrl };
        }
        // --- end self-update ---

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
            Console.WriteLine("  file mkdir   Create directory");
            Console.WriteLine("  file delete  Delete file or directory");
            Console.WriteLine("  file copy    Copy file");
            Console.WriteLine();
            Console.WriteLine("Process Module:");
            Console.WriteLine("  process.run  Execute process (async, fire-and-forget)");
            Console.WriteLine("  process.exec Execute process (sync, with output capture)");
            Console.WriteLine();
            Console.WriteLine("Registry Module:");
            Console.WriteLine("  reg set      Set registry value");
            Console.WriteLine("  reg get      Get registry value");
            Console.WriteLine("  reg delete   Delete value or key");
            Console.WriteLine();
            Console.WriteLine("Sleep Module:");
            Console.WriteLine("  sleep ms     Sleep milliseconds (sleep.ms ms=<num>)");
            Console.WriteLine("  sleep sec    Sleep seconds (sleep.sec sec=<num>)");
            Console.WriteLine();
            Console.WriteLine("Net Module:");
            Console.WriteLine("  net download Download file from URL");
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
