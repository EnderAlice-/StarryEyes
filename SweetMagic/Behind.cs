﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SweetMagic
{
    public partial class Behind : Form
    {
        private string callbackPoint = null;

        public Behind()
        {
            InitializeComponent();
        }

        private Process StartCoProcess(bool superUser)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
            };
            var cmd = Environment.GetCommandLineArgs();
            // Command parameters is
            // [File], [Version], [Key File], [Update Base Path], [Process ID]
            if (cmd.Length < 5)
            {
                MessageBox.Show("Information losted.", "Updater", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                Application.Exit();
                return null;
            }
            callbackPoint = cmd[3];
            startInfo.Arguments = String.Join(" ", cmd.Skip(1).Concat(new[] { "runas" }).Select(s => "\"" + s + "\""));
            if (superUser && Environment.OSVersion.Version.Major >= 6)
            {
                startInfo.Verb = "runas";
            }
            try
            {
                var process = new Process();
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                if (!process.Start())
                {
                    throw new Exception("Startup faied");
                }
                return process;
            }
            catch
            {
                return null;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            var cmd = Environment.GetCommandLineArgs();
            if (cmd.Length < 2)
            {
                MessageBox.Show("Information losted.", "Updater", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                Application.Exit();
                return;
            }
            var p = StartCoProcess(true);
            while (p == null)
            {
                var result =
                MessageBox.Show(
                    "Fail to start updater with elevated authority." + Environment.NewLine +
                    "If you want to retry, press Retry." + Environment.NewLine +
                    "Or, if you want to run update without superuser, press Ignore." + Environment.NewLine +
                    "Otherwise, you can cancel update by clicking Abort.",
                    "Krile updater - Execution error",
                     MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);
                if (result == DialogResult.Abort)
                {
                    break;
                }
                p = this.StartCoProcess(result != DialogResult.Ignore);
            }
            if (p == null)
            {
                Application.Exit();
            }
            else
            {
                p.EnableRaisingEvents = true;
                p.SynchronizingObject = this;
                p.Exited += this.p_Exited;
            }
        }

        void p_Exited(object sender, EventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo(Path.Combine(callbackPoint, Program.CallbackFile))
                {
                    UseShellExecute = true,
                    Arguments = "-postupdate",
                    WorkingDirectory = callbackPoint,
                };
                Process.Start(psi);
            }
            finally
            {
                Application.Exit();
            }
        }

    }
}
