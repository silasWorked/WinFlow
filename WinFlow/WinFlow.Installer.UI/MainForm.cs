using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFlow.Installer.Cli;

namespace WinFlow.Installer.UI
{
    public partial class MainForm : Form
    {
        private readonly InstallerService _installer = new InstallerService();

        public MainForm()
        {
            InitializeComponent();
            txtInstallDir.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinFlow");
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = txtInstallDir.Text;
            if (dlg.ShowDialog() == DialogResult.OK) txtInstallDir.Text = dlg.SelectedPath;
        }

        private async void btnInstall_Click(object sender, EventArgs e)
        {
            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;
            var opts = new InstallOptions
            {
                NoAssoc = !chkAssoc.Checked,
                NoPath = !chkPath.Checked,
                CreateDesktopDemo = chkDemo.Checked,
                AssocTarget = string.IsNullOrWhiteSpace(txtAssocTarget.Text) ? null : txtAssocTarget.Text
            };

            var progress = new Progress<string>(s => AppendLog(s));
            try
            {
                await _installer.InstallAsync(txtInstallDir.Text, opts, progress);
            }
            catch (Exception ex)
            {
                AppendLog("ERROR: " + ex.Message);
            }
            finally
            {
                btnInstall.Enabled = true;
                btnUninstall.Enabled = true;
            }
        }

        private async void btnUninstall_Click(object sender, EventArgs e)
        {
            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;
            var progress = new Progress<string>(s => AppendLog(s));
            await _installer.UninstallAsync(txtInstallDir.Text, progress);
            btnInstall.Enabled = true;
            btnUninstall.Enabled = true;
        }

        private void AppendLog(string s)
        {
            txtLog.AppendText(s + Environment.NewLine);
        }
    }
}
