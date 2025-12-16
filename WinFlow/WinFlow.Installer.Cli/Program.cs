using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinFlow.Installer.Cli
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			Console.WriteLine("WinFlow Installer (console)");

			var doUninstall = HasArg(args, "--uninstall");
			var createDesktopDemo = HasArg(args, "--create-desktop-demo");
			var noAssoc = HasArg(args, "--no-assoc");
			var noPath = HasArg(args, "--no-path");
			var installDir = GetArgValue(args, "--dir") ??
							 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinFlow");

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Console.Error.WriteLine("This installer supports Windows only.");
				return 1;
			}

			if (doUninstall)
			{
				return Uninstall(installDir);
			}

			Console.WriteLine($"Install directory: {installDir}");
			Directory.CreateDirectory(installDir);

			// Detect CLI binary near installer (release package scenario)
			var selfDir = AppContext.BaseDirectory;
			var cliCandidates = new[]
			{
				Path.Combine(selfDir, "WinFlow.Cli.exe"),
				Path.Combine(selfDir, "winflow.exe"),
			};
			string? cliSource = null;
			foreach (var c in cliCandidates)
				if (File.Exists(c)) { cliSource = c; break; }

			if (cliSource == null)
			{
				Console.WriteLine("CLI executable not found next to installer. Attempting to locate dev build...");
				// Dev fallback: look in repo output
				var dev = Path.Combine(selfDir, "..", "..", "WinFlow.Cli", "bin", "Debug", "net8.0", "WinFlow.Cli.exe");
				var devFull = Path.GetFullPath(dev);
				if (File.Exists(devFull)) cliSource = devFull;
			}

			if (cliSource == null)
			{
				Console.Error.WriteLine("WinFlow.Cli.exe not found. Place installer next to WinFlow.Cli.exe or build the project.");
				return 2;
			}

			var cliTarget = Path.Combine(installDir, "winflow.exe");
			File.Copy(cliSource, cliTarget, overwrite: true);
			Console.WriteLine($"Copied CLI -> {cliTarget}");

			if (!noAssoc)
			{
				try
				{
					RegisterAssociation(cliTarget);
					Console.WriteLine(".wflow associated to WinFlow.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Association failed: {ex.Message}");
				}
			}

			if (!noPath)
			{
				try
				{
					EnsureUserPath(installDir);
					BroadcastEnvChange();
					Console.WriteLine("Install directory added to user PATH.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"PATH update failed: {ex.Message}");
				}
			}

			if (createDesktopDemo)
			{
				try
				{
					CreateDesktopDemo();
					Console.WriteLine("Desktop demo script created.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Demo creation failed: {ex.Message}");
				}
			}

			Console.WriteLine("Installation complete.");
			return 0;
		}

		private static int Uninstall(string installDir)
		{
			try
			{
				UnregisterAssociation();
				Console.WriteLine("Association removed.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Association removal failed: {ex.Message}");
			}

			try
			{
				RemoveFromUserPath(installDir);
				BroadcastEnvChange();
				Console.WriteLine("Removed from user PATH.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"PATH cleanup failed: {ex.Message}");
			}

			try
			{
				if (Directory.Exists(installDir))
				{
					foreach (var f in Directory.GetFiles(installDir))
					{
						try { File.Delete(f); } catch { }
					}
					Directory.Delete(installDir, recursive: true);
				}
				Console.WriteLine("Install directory removed.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Directory cleanup failed: {ex.Message}");
			}

			Console.WriteLine("Uninstall complete.");
			return 0;
		}

		private static void RegisterAssociation(string cliPath)
		{
			var progId = "WinFlow.Script";
			using var ext = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.wflow");
			ext!.SetValue(null, progId);
			using var prog = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
			prog!.SetValue(null, "WinFlow Script");
			using var icon = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\DefaultIcon");
			icon!.SetValue(null, "shell32.dll,70");
			using var cmd = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command");
			cmd!.SetValue(null, $"\"{cliPath}\" \"%1\"");
		}

		private static void UnregisterAssociation()
		{
			var progId = "WinFlow.Script";
			try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.wflow"); } catch { }
			try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}"); } catch { }
		}

		private static void EnsureUserPath(string dir)
		{
			var current = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;
			var parts = current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (var p in parts)
				if (string.Equals(p, dir, StringComparison.OrdinalIgnoreCase))
					return; // already present
			var updated = string.IsNullOrEmpty(current) ? dir : current + ";" + dir;
			Environment.SetEnvironmentVariable("Path", updated, EnvironmentVariableTarget.User);
		}

		private static void RemoveFromUserPath(string dir)
		{
			var current = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;
			var parts = new System.Collections.Generic.List<string>();
			foreach (var p in current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				if (!string.Equals(p, dir, StringComparison.OrdinalIgnoreCase)) parts.Add(p);
			var updated = string.Join(";", parts);
			Environment.SetEnvironmentVariable("Path", updated, EnvironmentVariableTarget.User);
		}

		private static void CreateDesktopDemo()
		{
			var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			var path = Path.Combine(desktop, "WinFlow Demo.wflow");
			var content = "// WinFlow demo\n# Comments and blank lines are ignored\n\n" +
						  "echo \"Hello from WinFlow!\"\nnoop\necho \"Done.\"\n";
			File.WriteAllText(path, content);
		}

		private static bool HasArg(string[] args, string name)
		{
			foreach (var a in args)
				if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		private static string? GetArgValue(string[] args, string name)
		{
			for (int i = 0; i < args.Length - 1; i++)
				if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
			return null;
		}

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
			uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

		private static void BroadcastEnvChange()
		{
			const uint HWND_BROADCAST = 0xFFFF;
			const uint WM_SETTINGCHANGE = 0x001A;
			const uint SMTO_ABORTIFHUNG = 0x0002;
			SendMessageTimeout(new IntPtr(HWND_BROADCAST), WM_SETTINGCHANGE, IntPtr.Zero, "Environment",
				SMTO_ABORTIFHUNG, 5000, out _);
		}
	}
}
