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
using System.IO;

namespace ADBLoggingTool
{
    public partial class Form1 : Form
    {
        Process p = new Process();
        bool serverStarted = false;
        bool isLogging = false;
        bool endThread = false;
        String fileName = "test.txt";
        string output = "";
        string pathString;
        public Form1()
        {
            InitializeComponent();
        }

        String run_process(string commands)
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
            output = p.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            return output;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            String ipAddress = ipAddressTB.Text;
            fileName = fileNameTB.Text;
            if (!validateIPAddress(ipAddress))
            {
                MessageBox.Show("Invalid IP Address, please try again.");
                return;
            }
            if (fileName.Equals("") || fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid file name, please try again.");
                return;
            }
            setupPath();
            if (pathString.Equals("")){
                return;
            }
            startBtn.Enabled = false;
            Status.Text = ("Starting Server");
            run_process("adb start-server");
            Status.Text = ("Server Started");   
            serverStarted = true;
            Status.Text = ("Connecting to : " + ipAddress);
            String adbConnectCall = run_process("adb connect " + ipAddress);
            if (adbConnectCall.Contains("failed"))
            {
                MessageBox.Show("Connection Failed");
                Status.Text = ("");
                return;
            }
            Status.Text = ("Cleaning past logs");
            run_process("adb logcat -c");
            stopBtn.Enabled = true;
            Status.Text = ("Logging IP Address: " + ipAddress);
            new Thread(() =>
            {
                if (endThread)
                {
                   return;
                }
                isLogging = true;
                output = run_process("adb logcat");
                if (!output.Equals(""))
                {
                    writeLogs();
                }
            }).Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (isLogging)
            {
                endThread = true;
            }
            if (serverStarted)
            {
                Process stop = new Process();
                stop.StartInfo.FileName = "cmd.exe";
                stop.StartInfo.Arguments = "/c " + "adb kill-server";
                stop.StartInfo.CreateNoWindow = true;
                stop.StartInfo.UseShellExecute = false;
                stop.StartInfo.RedirectStandardOutput = true;
                stop.Start();
                p.Kill();
                if (isLogging)
                {
                    Status.Text = "Logging complete, file saved";
                    isLogging = false;
                    endThread = false;
                }
            }
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

        private void writeLogs()
        {
            setupPath();
            if (!pathString.Equals(""))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(pathString))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(output);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private void setupPath()
        {
            string folderName = @System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            pathString = System.IO.Path.Combine(folderName, "AndroidLogs");
            System.IO.Directory.CreateDirectory(pathString);
            pathString = System.IO.Path.Combine(pathString, fileName + ".txt");
            Console.WriteLine("Path to my file: {0}\n", pathString);
            if (System.IO.File.Exists(pathString))
            {
                MessageBox.Show(fileName+".txt already exists, please enter a different name");
                pathString = "";
                return;
            }
        }
    }
}