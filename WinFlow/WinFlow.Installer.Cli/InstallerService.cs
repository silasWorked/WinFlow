using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace WinFlow.Installer.Cli
{
    [SupportedOSPlatform("windows")]
    public class InstallOptions
    {
        public bool NoAssoc { get; set; }
        public bool NoPath { get; set; }
        public bool CreateDesktopDemo { get; set; }
        public string? AssocTarget { get; set; }
    }

    [SupportedOSPlatform("windows")]
    public class InstallerService
    {
        public async Task InstallAsync(string installDir, InstallOptions opts, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            progress?.Report($"Installing to {installDir}");
            Directory.CreateDirectory(installDir);

            // Locate CLI: respect explicit target
            string? cliSource = opts.AssocTarget;
            if (string.IsNullOrWhiteSpace(cliSource))
            {
                var selfDir = AppContext.BaseDirectory;
                var cliCandidates = new[] { Path.Combine(selfDir, "WinFlow.Cli.exe"), Path.Combine(selfDir, "winflow.exe") };
                foreach (var c in cliCandidates)
                    if (File.Exists(c)) { cliSource = c; break; }
            }

            if (string.IsNullOrWhiteSpace(cliSource) || !File.Exists(cliSource))
                throw new InvalidOperationException("WinFlow.Cli executable not found. Provide path via options.AssocTarget or place installer next to CLI.");

            var cliTarget = Path.Combine(installDir, "winflow.exe");
            await Task.Run(() => File.Copy(cliSource!, cliTarget, overwrite: true), ct);
            progress?.Report($"Copied CLI -> {cliTarget}");

            // ShellHost optional
            var shellSrc = Path.Combine(AppContext.BaseDirectory, "WinFlow.ShellHost.exe");
            if (File.Exists(shellSrc))
            {
                var shellTarget = Path.Combine(installDir, "WinFlow.ShellHost.exe");
                File.Copy(shellSrc, shellTarget, overwrite: true);
                progress?.Report($"Copied ShellHost -> {shellTarget}");
            }
            else
            {
                progress?.Report("ShellHost not found; interactive shell not available.");
            }

            if (!opts.NoAssoc)
            {
                try
                {
                    WindowsFileAssociation.Register(opts.AssocTarget ?? cliTarget);
                    progress?.Report(".wflow associated to WinFlow.");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Association failed: {ex.Message}");
                }
            }

            if (!opts.NoPath)
            {
                try
                {
                    var current = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;
                    var parts = current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var p in parts)
                        if (string.Equals(p, installDir, StringComparison.OrdinalIgnoreCase)) goto skipPath;
                    var updated = string.IsNullOrEmpty(current) ? installDir : current + ";" + installDir;
                    Environment.SetEnvironmentVariable("Path", updated, EnvironmentVariableTarget.User);
                    BroadcastEnvChange();
                    progress?.Report("Install directory added to user PATH.");
                }
                catch (Exception ex)
                {
                    progress?.Report($"PATH update failed: {ex.Message}");
                }
            }

            skipPath:;

            if (opts.CreateDesktopDemo)
            {
                try
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var path = Path.Combine(desktop, "WinFlow Demo.wflow");
                    var content = "// WinFlow demo\n# Comments and blank lines are ignored\n\n" +
                                  "echo \"Hello from WinFlow!\"\nnoop\necho \"Done.\"\n";
                    await File.WriteAllTextAsync(path, content, ct);
                    progress?.Report("Desktop demo script created.");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Demo creation failed: {ex.Message}");
                }
            }

            progress?.Report("Installation complete.");
        }

        public Task UninstallAsync(string installDir, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                try
                {
                    WindowsFileAssociation.Unregister();
                    progress?.Report("Association removed.");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Association removal failed: {ex.Message}");
                }

                try
                {
                    var current = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var p in current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        if (!string.Equals(p, installDir, StringComparison.OrdinalIgnoreCase)) parts.Add(p);
                    var updated = string.Join(";", parts);
                    Environment.SetEnvironmentVariable("Path", updated, EnvironmentVariableTarget.User);
                    BroadcastEnvChange();
                    progress?.Report("Removed from user PATH.");
                }
                catch (Exception ex)
                {
                    progress?.Report($"PATH cleanup failed: {ex.Message}");
                }

                try
                {
                    if (Directory.Exists(installDir))
                    {
                        foreach (var f in Directory.GetFiles(installDir))
                        {
                            try { File.Delete(f); } catch (Exception ex) { progress?.Report($"Warning: failed to delete {f}: {ex.Message}"); }
                        }
                        Directory.Delete(installDir, recursive: true);
                    }
                    progress?.Report("Install directory removed.");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Directory cleanup failed: {ex.Message}");
                }

                progress?.Report("Uninstall complete.");
            }, ct);
        }

        private static void BroadcastEnvChange()
        {
            const uint HWND_BROADCAST = 0xFFFF;
            const uint WM_SETTINGCHANGE = 0x001A;
            const uint SMTO_ABORTIFHUNG = 0x0002;
            SendMessageTimeout(new IntPtr(HWND_BROADCAST), WM_SETTINGCHANGE, IntPtr.Zero, "Environment",
                SMTO_ABORTIFHUNG, 5000, out _);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
    }
}
