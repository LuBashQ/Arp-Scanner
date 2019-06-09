using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ConsoleScanner
{
    class ARPHelper
    {

        public ConcurrentQueue<IPAddress> qIPAddresses { get; set; }
        public XmlDocument oDocument { get; }
        public XmlNode oRoot { get; }
        public bool isFileEnabled { get; }
        public Database oDatabase { get; }

        public ARPHelper(ConcurrentQueue<IPAddress> qIPAddresses, bool isFileEnabled, Database oDatabase)
        {
            this.qIPAddresses = qIPAddresses;
            this.oDocument = null;
            this.oRoot = null;
            this.isFileEnabled = isFileEnabled;
            this.oDatabase = oDatabase;
        }

        public ARPHelper(ConcurrentQueue<IPAddress> qIPAddresses, XmlDocument oDocument, XmlNode oRoot, bool isFileEnabled, Database oDatabase)
            : this(qIPAddresses, isFileEnabled, oDatabase)
        {
            this.oDocument = oDocument;
            this.oRoot = oRoot;
        }

        public ARPHelper(XmlDocument oDocument, XmlNode oRoot, bool isFileEnabled, Database oDatabase)
        {
            qIPAddresses = new ConcurrentQueue<IPAddress>();
            this.oDocument = oDocument;
            this.oDatabase = oDatabase;
            this.isFileEnabled = isFileEnabled;
            this.oRoot = oRoot;
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

        public void GetMAC()
        {
            IPAddress oIpAddress;
            GetNextIP(out oIpAddress);
            int iIpAddress = BitConverter.ToInt32(oIpAddress.GetAddressBytes(), 0);
            byte[] baMacAddress = new byte[6];
            int iMacLength = baMacAddress.Length;

            ProcessARPRequest(iIpAddress, baMacAddress, iMacLength, oIpAddress);

            Thread.CurrentThread.Abort();
        }

        private void ProcessARPRequest(int iIpAddress, byte[] baMacAddress, int iMacLength, IPAddress oIpAddress)
        {
            int isArpRequestOk = SendARP(iIpAddress, 0, baMacAddress, ref iMacLength);

            if (isArpRequestOk == 0)
            {
                string sHostname = "Unknown";
                string sMacAddress = ProcessMACAddress(iMacLength, baMacAddress);

                try
                {
                    sHostname = Dns.GetHostEntry(oIpAddress.ToString()).HostName;
                }
                catch { }

                if (isFileEnabled)
                {
                    SaveToFile(sHostname, oIpAddress, sMacAddress);
                }
                else
                {
                    SaveToDatabase(sHostname, oIpAddress, sMacAddress);
                }
            }
        }

        private void GetNextIP(out IPAddress oIpAddress)
        {
            qIPAddresses.TryDequeue(out oIpAddress);
        }

        private string ProcessMACAddress(int macAddrLen, byte[] macAddr)
        {
            string[] str = new string[(int)macAddrLen];

            for (int i = 0; i < macAddrLen; i++)
                str[i] = macAddr[i].ToString("x2");

            return string.Join("-", str);
        }

        public void SaveToFile(string sHostname, IPAddress oIpAddress, string sMacAddress)
        {
            XmlNode oHost = oDocument.CreateElement("host");
            XmlNode oHostname = oDocument.CreateElement("name");
            XmlNode oHostIp = oDocument.CreateElement("ip");
            XmlNode oHostMac = oDocument.CreateElement("MAC");

            lock (oDocument)
            {
                oHostname.AppendChild(oDocument.CreateTextNode(sHostname));
                oHostIp.AppendChild(oDocument.CreateTextNode(oIpAddress.ToString()));
                oHostMac.AppendChild(oDocument.CreateTextNode(sMacAddress));
                oHost.AppendChild(oHostname);
                oHost.AppendChild(oHostIp);
                oHost.AppendChild(oHostMac);
                oRoot.AppendChild(oHost);
            }
        }

        private void SaveToDatabase(string hostname, IPAddress address, string macAddress)
        {
            lock (oDatabase)
            {
                if (!oDatabase.ExecuteQuery("SELECT * FROM computers WHERE MAC = @mac", new List<MySqlParameter>() { new MySqlParameter("@mac", macAddress) }))
                {
                    oDatabase.ExecuteQuery("INSERT INTO computers(MAC,IP,hostname) VALUES(@mac,@ip,@hostname)",
                        new List<MySqlParameter>()
                        {
                                new MySqlParameter("@mac",macAddress),
                                new MySqlParameter("@ip",address.ToString()),
                                new MySqlParameter("@hostname",hostname)
                        });
                }
                oDatabase.ExecuteQuery("INSERT INTO last_online(MAC,date) VALUES(@mac,@date)",
                        new List<MySqlParameter>()
                        {
                                new MySqlParameter("@mac",macAddress),
                                new MySqlParameter("@date",DateTime.Now)
                        });
            }
        }


        public void EnqueueIpAddress(string sIpAddress)
        {
            qIPAddresses.Enqueue(IPAddress.Parse(sIpAddress));
        }

    }
}
