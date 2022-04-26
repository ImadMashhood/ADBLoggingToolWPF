using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

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
        List<String> connectedDevices;
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
            if(connectedDevices.Count <= 1)
            {
                ipAddress = ipAddressTB.Text;

            }
            else
            {
                ipAddress = ipAddressCB.Text;
            }
            fileName = fileNameTB.Text;
            //Input Validation for File Name
            if (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid file name, please try again.");
                return;
            }
            //If no name is entered fill name with current timestamp
            if(fileName == "")
            {
                fileName = DateTime.Now.ToString("yyyyMMddHHmmss")+"-logs";
            }
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
                run_process("adb logcat > "+fileName+".txt");
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


        //On Close method
        private void SaveProps()
        {
            //Save Settings
            if(connectedDevices.Count <= 1)
            {
                Properties.Settings.Default.ipAddress = ipAddressTB.Text;
            }
            else
            {
                Properties.Settings.Default.ipAddress = ipAddressCB.Text;
            }
            Properties.Settings.Default.prevCheckedLogs = clearPrevLogsCB.Checked;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
            //Reconnect to all devices
            foreach(String connectedDevice in connectedDevices)
            {
                run_process("adb connect " + connectedDevice);
            }
        }

        //On Load Method
        private void Form1_Load(object sender, EventArgs e)
        {
            //Get Settings
            //Check total amount of connected devices and enable the correct fields
            connectedDevices = checkConnectedDevices();
            setupIPFields();
        }

        private void setupIPFields()
        {
            if (connectedDevices.Count <= 1)
            {
                ipAddressTB.Text = Properties.Settings.Default.ipAddress;
                ipAddressCB.Visible = false;
                ipAddressTB.Visible = true;
            }
            else
            {
                string test = Properties.Settings.Default.ipAddress;
                ipAddressTB.Visible = false;
                ipAddressCB.Visible = true;
                foreach (String connectedDevice in connectedDevices)
                {
                    ipAddressCB.Items.Add(connectedDevice);
                }
            }
            clearPrevLogsCB.Checked = Properties.Settings.Default.prevCheckedLogs;
        }

        private List<String> checkConnectedDevices()
        {
            List<String> connectedDevices = new List<String>();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + "adb devices";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            //We ignore the first line since its just a title
            while (!p.StandardOutput.EndOfStream)
            {
                output = (p.StandardOutput.ReadLine());
                if(output.Contains("List") || output.Equals(""))
                {
                    continue;
                }
                string[] outputArray = output.Split(':');
                Console.WriteLine(outputArray[0]);
                connectedDevices.Add(outputArray[0]);
            }
            return connectedDevices;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveProps();
        }
    }
}