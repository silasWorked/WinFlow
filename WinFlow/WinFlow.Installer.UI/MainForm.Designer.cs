namespace WinFlow.Installer.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtInstallDir;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.CheckBox chkAssoc;
        private System.Windows.Forms.CheckBox chkPath;
        private System.Windows.Forms.CheckBox chkDemo;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.TextBox txtAssocTarget;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtInstallDir = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.chkAssoc = new System.Windows.Forms.CheckBox();
            this.chkPath = new System.Windows.Forms.CheckBox();
            this.chkDemo = new System.Windows.Forms.CheckBox();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.txtAssocTarget = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtInstallDir
            // 
            this.txtInstallDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInstallDir.Location = new System.Drawing.Point(12, 12);
            this.txtInstallDir.Name = "txtInstallDir";
            this.txtInstallDir.Size = new System.Drawing.Size(400, 23);
            this.txtInstallDir.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(418, 12);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // chkAssoc
            // 
            this.chkAssoc.AutoSize = true;
            this.chkAssoc.Checked = true;
            this.chkAssoc.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAssoc.Location = new System.Drawing.Point(12, 50);
            this.chkAssoc.Name = "chkAssoc";
            this.chkAssoc.Size = new System.Drawing.Size(156, 19);
            this.chkAssoc.TabIndex = 2;
            this.chkAssoc.Text = "Register .wflow association";
            this.chkAssoc.UseVisualStyleBackColor = true;
            // 
            // chkPath
            // 
            this.chkPath.AutoSize = true;
            this.chkPath.Checked = true;
            this.chkPath.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPath.Location = new System.Drawing.Point(12, 75);
            this.chkPath.Name = "chkPath";
            this.chkPath.Size = new System.Drawing.Size(118, 19);
            this.chkPath.TabIndex = 3;
            this.chkPath.Text = "Add to user PATH";
            this.chkPath.UseVisualStyleBackColor = true;
            // 
            // chkDemo
            // 
            this.chkDemo.AutoSize = true;
            this.chkDemo.Location = new System.Drawing.Point(12, 100);
            this.chkDemo.Name = "chkDemo";
            this.chkDemo.Size = new System.Drawing.Size(148, 19);
            this.chkDemo.TabIndex = 4;
            this.chkDemo.Text = "Create desktop demo";
            this.chkDemo.UseVisualStyleBackColor = true;
            // 
            // txtAssocTarget
            // 
            this.txtAssocTarget.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAssocTarget.Location = new System.Drawing.Point(12, 130);
            this.txtAssocTarget.Name = "txtAssocTarget";
            this.txtAssocTarget.PlaceholderText = "Optional: explicit path to winflow.exe for association";
            this.txtAssocTarget.Size = new System.Drawing.Size(481, 23);
            this.txtAssocTarget.TabIndex = 5;
            // 
            // btnInstall
            // 
            this.btnInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInstall.Location = new System.Drawing.Point(418, 300);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(75, 23);
            this.btnInstall.TabIndex = 6;
            this.btnInstall.Text = "Install";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // btnUninstall
            // 
            this.btnUninstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUninstall.Location = new System.Drawing.Point(337, 300);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new System.Drawing.Size(75, 23);
            this.btnUninstall.TabIndex = 7;
            this.btnUninstall.Text = "Uninstall";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 170);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(481, 120);
            this.txtLog.TabIndex = 8;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(505, 335);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnUninstall);
            this.Controls.Add(this.btnInstall);
            this.Controls.Add(this.txtAssocTarget);
            this.Controls.Add(this.chkDemo);
            this.Controls.Add(this.chkPath);
            this.Controls.Add(this.chkAssoc);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtInstallDir);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "WinFlow Installer";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}