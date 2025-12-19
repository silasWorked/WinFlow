using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace WinFlow.Installer.Cli
{
    [SupportedOSPlatform("windows")]
    internal static class WindowsFileAssociation
    {
        private const string ProgId = "WinFlow.Script";

        public static void Register(string cliPath)
        {
            if (string.IsNullOrWhiteSpace(cliPath)) throw new ArgumentException("cliPath is required", nameof(cliPath));

            using var ext = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.wflow");
            ext!.SetValue(null, ProgId);

            using var prog = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}");
            prog!.SetValue(null, "WinFlow Script");

            using var icon = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon");
            icon!.SetValue(null, "shell32.dll,70");

            // Default open command
            using var cmd = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command");
            cmd!.SetValue(null, $"\"{cliPath}\" \"%1\"");

            // Add explicit verbs for context menu: Run and Debug
            using var runVerb = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\run");
            runVerb!.SetValue(null, "Run with WinFlow");
            using var runCmd = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\run\command");
            runCmd!.SetValue(null, $"\"{cliPath}\" run \"%1\"");

            using var debugVerb = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\debug");
            debugVerb!.SetValue(null, "Debug with WinFlow");
            using var debugCmd = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\debug\command");
            debugCmd!.SetValue(null, $"\"{cliPath}\" debug \"%1\"");
        }

        public static void Unregister()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.wflow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to remove .wflow association: {ex.Message}");
            }

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to remove ProgID {ProgId}: {ex.Message}");
            }
        }
    }
}
