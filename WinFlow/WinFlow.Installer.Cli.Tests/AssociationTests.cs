using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Xunit;

namespace WinFlow.Installer.Cli.Tests
{
    public class AssociationTests
    {
        [Fact]
        public void RegisterAndUnregister_AssociationRoundtrip()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // skip on non-windows

            var fakeCli = Path.Combine(Path.GetTempPath(), "winflow-fake.exe");
            File.WriteAllText(fakeCli, "echo fake");

            try
            {
                WindowsFileAssociation.Register(fakeCli);

                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.wflow");
                Assert.NotNull(key);
                var prog = key.GetValue(null) as string;
                Assert.Equal("WinFlow.Script", prog);

                using var progKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\WinFlow.Script\shell\open\command");
                Assert.NotNull(progKey);
                var cmd = progKey.GetValue(null) as string;
                Assert.Contains("winflow-fake.exe", cmd, StringComparison.OrdinalIgnoreCase);

                using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\WinFlow.Script\shell\run\command");
                Assert.NotNull(runKey);
                var runCmd = runKey.GetValue(null) as string;
                Assert.Contains("run", runCmd, StringComparison.OrdinalIgnoreCase);

                using var debugKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\WinFlow.Script\shell\debug\command");
                Assert.NotNull(debugKey);
                var debugCmd = debugKey.GetValue(null) as string;
                Assert.Contains("debug", debugCmd, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                // cleanup
                WindowsFileAssociation.Unregister();
                if (File.Exists(fakeCli)) File.Delete(fakeCli);
            }
        }
    }
}
