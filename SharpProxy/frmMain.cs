﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SharpProxy
{
    public partial class frmMain : Form
    {
        protected const int MIN_PORT = 1;
        protected const int MAX_PORT = 65535;

        protected ProxyThread ProxyThreadListener = null;

        public frmMain()
        {
            InitializeComponent();
            this.Text += " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string ip = getLocalIP();
            if(!string.IsNullOrEmpty(ip))
                lblMyIP.Text = ip;

            int port = 5000;
            while (!checkPortAvailability(port))
            {
                port++;
            }
            txtExternalPort.Text = port.ToString();
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            txtInternalPort.Focus();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ProxyThreadListener != null)
                ProxyThreadListener.Stop();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            int externalPort = 0;
            int internalPort = 0;
            //Validation
            int.TryParse(txtExternalPort.Text, out externalPort);
            int.TryParse(txtInternalPort.Text, out internalPort);
            if (!checkPortRange(externalPort)
                || !checkPortRange(internalPort)
                || externalPort == internalPort)
            {
                showError("Ports must be between " + MIN_PORT + "-" + MAX_PORT + " and must not be the same.");
                return;
            }
            if (!checkPortAvailability(externalPort))
            {
                showError("Port " + externalPort + " is not available, please select a different port.");
                return;
            }

            ProxyThreadListener = new ProxyThread(externalPort, internalPort);

            toggleButtons();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            ProxyThreadListener.Stop();

            toggleButtons();
        }

        private void showError(string msg)
        {
            MessageBox.Show(msg, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected bool checkPortRange(int port)
        {
            if (port < MIN_PORT || port > MAX_PORT)
                return false;
            return true;
        }

        protected string getLocalIP()
        {
            //Try to find our internal IP address...
            string myHost = System.Net.Dns.GetHostName();
            IPAddress[] addresses = System.Net.Dns.GetHostEntry(myHost).AddressList;
            string myIP = "";
            string fallbackIP = "";

            for (int i = 0; i < addresses.Length; i++)
            {
                //Is this a valid IPv4 address?
                if (addresses[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string thisAddress = addresses[i].ToString();
                    //Loopback is not our preference...
                    if (thisAddress == "127.0.0.1")
                        continue;
                    //169.x.x.x addresses are self-assigned "private network" IP by Windows
                    if (thisAddress.StartsWith("169"))
                    {
                        fallbackIP = thisAddress;
                        continue;
                    }
                    myIP = thisAddress;
                    break;
                }
            }
            if (string.IsNullOrEmpty(myIP) && !string.IsNullOrEmpty(fallbackIP))
            {
                myIP = fallbackIP;
            }

            return myIP;
        }

        private void toggleButtons()
        {
            btnStop.Enabled = !btnStop.Enabled;
            btnStart.Enabled = !btnStart.Enabled;
            txtExternalPort.Enabled = !txtExternalPort.Enabled;
            txtInternalPort.Enabled = !txtInternalPort.Enabled;
        }

        private bool checkPortAvailability(int port)
        {
            //http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                    return false;
            }

            try
            {
                TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                listener.Start();
                listener.Stop();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
