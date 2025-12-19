using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
			var doRegisterAssoc = HasArg(args, "--register-assoc");
			var doUnregisterAssoc = HasArg(args, "--unregister-assoc");
			var installDir = GetArgValue(args, "--dir") ??
					 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinFlow");

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Console.Error.WriteLine("This installer supports Windows only.");
				return 1;
			}

			if (doUnregisterAssoc)
			{
				try
				{
					WindowsFileAssociation.Unregister();
					Console.WriteLine("Association removed.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Association removal failed: {ex.Message}");
				}
				return 0;
			}

			if (doRegisterAssoc)
			{
				try
				{
					// Allow explicit target via --assoc-target or fallback to installer dir
					var target = GetArgValue(args, "--assoc-target");
					string? cliPath;
					if (!string.IsNullOrWhiteSpace(target))
					{
						cliPath = target;
						if (!File.Exists(cliPath)) throw new InvalidOperationException($"Provided assoc-target not found: {cliPath}");
					}
					else
					{
						var regSelfDir = AppContext.BaseDirectory;
						var regCliCandidates = new[]
						{
							Path.Combine(regSelfDir, "WinFlow.Cli.exe"),
							Path.Combine(regSelfDir, "winflow.exe"),
						};
						cliPath = null;
						foreach (var c in regCliCandidates)
							if (File.Exists(c)) { cliPath = c; break; }
						if (cliPath == null)
							throw new InvalidOperationException("CLI executable not found next to installer. Provide path via --assoc-target or place installer next to CLI.");
					}

					WindowsFileAssociation.Register(cliPath!);
					Console.WriteLine("Association registered.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Association registration failed: {ex.Message}");
				}
				return 0;
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


			// Try detect ShellHost executable near installer
			string? shellSource = null;
			var shellCandidates = new[]
			{
				Path.Combine(selfDir, "WinFlow.ShellHost.exe"),
			};
			foreach (var s in shellCandidates)
				if (File.Exists(s)) { shellSource = s; break; }

			if (shellSource == null)
			{
				// Dev fallback for ShellHost
				var devShell = Path.Combine(selfDir, "..", "..", "WinFlow.ShellHost", "bin", "Debug", "net8.0", "WinFlow.ShellHost.exe");
				var devShellFull = Path.GetFullPath(devShell);
				if (File.Exists(devShellFull)) shellSource = devShellFull;
			}

			// Check for running WinFlow processes and ask to close them
			KillRunningProcessesIfNeeded();

			var cliTarget = Path.Combine(installDir, "winflow.exe");
			File.Copy(cliSource, cliTarget, overwrite: true);
			Console.WriteLine($"Copied CLI -> {cliTarget}");

			if (shellSource != null)
			{
				var shellTarget = Path.Combine(installDir, "WinFlow.ShellHost.exe");
				File.Copy(shellSource, shellTarget, overwrite: true);
				Console.WriteLine($"Copied ShellHost -> {shellTarget}");
			}
			else
			{
				Console.WriteLine("Warning: WinFlow.ShellHost.exe not found; interactive shell won't be available.");
			}

			if (!noAssoc)
			{
				try
				{
					WindowsFileAssociation.Register(cliTarget);
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
				WindowsFileAssociation.Unregister();
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
						try { File.Delete(f); } catch (Exception ex) { Console.WriteLine($"Warning: failed to delete {f}: {ex.Message}"); }
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

		private static void KillRunningProcessesIfNeeded()
		{
			// Find running WinFlow processes
			var winFlowProcesses = new List<Process>();
			try
			{
				var allProcesses = Process.GetProcesses();
				var names = new[] { "winflow", "WinFlow.Cli", "WinFlow.ShellHost" };
				
				foreach (var proc in allProcesses)
				{
					try
					{
						var procName = proc.ProcessName;
						if (names.Any(n => procName.Equals(n, StringComparison.OrdinalIgnoreCase)))
						{
							winFlowProcesses.Add(proc);
						}
					}
					catch { /* Ignore processes we can't access */ }
				}
			}
			catch { /* Ignore if we can't enumerate processes */ }

			if (winFlowProcesses.Count == 0) return;

			Console.WriteLine();
			Console.WriteLine($"Found {winFlowProcesses.Count} running WinFlow process(es):");
			foreach (var proc in winFlowProcesses)
			{
				try
				{
					Console.WriteLine($"  - {proc.ProcessName} (PID {proc.Id})");
				}
				catch { }
			}

			Console.Write("\nClose these processes to proceed with update? (y/n): ");
			var response = Console.ReadLine();
			if (response == null || !response.Equals("y", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Update cancelled.");
				Environment.Exit(1);
			}

			// Kill all found processes
			foreach (var proc in winFlowProcesses)
			{
				try
				{
					proc.Kill();
					proc.WaitForExit(3000);
					Console.WriteLine($"Closed {proc.ProcessName}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Warning: Failed to close {proc.ProcessName}: {ex.Message}");
				}
			}

			Console.WriteLine("Ready to install.");
			Console.WriteLine();
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
