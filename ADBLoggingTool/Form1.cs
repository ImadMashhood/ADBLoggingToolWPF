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
        String ipAddress = "";
        String fileName = "";
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
            //Take in user inputs
            ipAddress = ipAddressTB.Text;
            fileName = fileNameTB.Text;
            //Input Validation for IP and File Name
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
            //If no name is entered fill name with current timestamp
            if(fileName == "")
            {
                fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
            }
            //Confirm if path exists
            setupPath("AndroidLogs");
            if (pathString.Equals("")){
                return;
            }
            fileNameTB.Text = (fileName+".txt");
            //Disable start button, stop button cant be enabled yet cause termination fo connection acts weird at times
            startBtn.Enabled = false;
            //Set up ADB Server
            Status.Text = ("Starting Server");
            run_process("adb start-server");
            Status.Text = ("Server Started");   
            serverStarted = true;
            //Once server is started attept connection to IP Address
            Status.Text = ("Connecting to : " + ipAddress);
            String adbConnectCall = run_process("adb connect " + ipAddress);
            if (adbConnectCall.Contains("failed"))
            {
                MessageBox.Show("Connection Failed");
                Status.Text = ("");
                return;
            }
            //If user wants to clear previous logs, clean it
            if (clearPrevLogsCB.Checked)
            {
                Status.Text = ("Cleaning past logs");
                run_process("adb logcat -c");
            }
            //Allow stopping now
            stopBtn.Enabled = true;
            Status.Text = ("Logging IP Address: " + ipAddress);
            //Start Logging Thread
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
                    //Write Logs to file
                    //TODO: Write Logs to file at the same time.
                    writeLogs();
                }
            }).Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //If Logging is taking place close the thread
            if (isLogging)
            {
                endThread = true;
            }
            //This will kill the server if the server is still running
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
            SaveProps();
        }

        //Simple IP Validation Method found on google
        private bool validateIPAddress(String ipAddress)
        {
            if (String.IsNullOrEmpty(ipAddress))
                return false;

            var items = ipAddress.Split('.');

            if (items.Length != 4)
                return false;
            return items.All(item => byte.TryParse(item, out _));
        }

        //Writes logs using FileStream
        private void writeLogs()
        {
            setupPath("AndroidLogs");
            if (!pathString.Equals(""))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(pathString))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(output);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        //Setups and checks the file path
        private void setupPath(String subfolder)
        {
            string folderName = @System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            pathString = System.IO.Path.Combine(folderName, subfolder);
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

        //On Close method
        private void SaveProps()
        {
            //Save Settings
            Properties.Settings.Default.ipAddress = ipAddressTB.Text;
            Properties.Settings.Default.prevCheckedLogs = clearPrevLogsCB.Checked;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        //On Load Method
        private void Form1_Load(object sender, EventArgs e)
        {
            //Get Settings
            ipAddressTB.Text = Properties.Settings.Default.ipAddress;
            clearPrevLogsCB.Checked = Properties.Settings.Default.prevCheckedLogs;
        }

    }
}