using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ADBLoggingTool
{
    public partial class Form1 : Form
    {
        Process p = new Process();
        bool endThread = false;
        public Form1()
        {
            InitializeComponent();
        }

        string run_process(string commands)
        {
            
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + commands;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            do
            {
                Application.DoEvents();
            } while (!p.HasExited); 
            return p.StandardOutput.ReadToEnd();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            String ipAddress = ipAddressTB.Text;
            String fileName = fileNameTB.Text;
            if (!validateIPAddress(ipAddress))
            {
                MessageBox.Show("Invalid IP Address, please try again.");
                return;
            }
            if (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid file name, please try again.");
                return;
            }
            startBtn.Enabled = false;
            stopBtn.Enabled = true;
            Status.Text = "Logging IP: " + ipAddress;
            new Thread(() =>
            {
                if (endThread)
                {
                    return;
                }
                Console.WriteLine(ipAddress);
                Console.WriteLine(run_process("adb start-server"));
                Console.WriteLine(run_process("adb connect"));
                Console.WriteLine(run_process("adb logcat"));
            }).Start();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            endThread = true;
            Process stop = new Process();
            stop.StartInfo.FileName = "cmd.exe";
            stop.StartInfo.Arguments = "/c " + "adb kill-server";
            stop.StartInfo.CreateNoWindow = true;
            stop.StartInfo.UseShellExecute = false;
            stop.StartInfo.RedirectStandardOutput = true;
            stop.Start();
            p.Kill();
            Status.Text = "Logging complete, file saved";
            endThread = false;
            stopBtn.Enabled = false;
            startBtn.Enabled = true;
        }

        private bool validateIPAddress(String ipAddress)
        {
            if (String.IsNullOrEmpty(ipAddress))
                return false;

            var items = ipAddress.Split('.');

            if (items.Length != 4)
                return false;
            return items.All(item => byte.TryParse(item, out _));
        }
    }
}