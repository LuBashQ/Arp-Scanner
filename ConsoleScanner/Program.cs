using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleScanner
{
    class Program
    {
        static void Main(string[] args)
        {

            
            Database oDatabase = oDatabase = new Database("localhost", "hosts", "lupascu", "lupascu", 3306);
            try
            {
                string sBaseSubnet = args[0];
                int iAddressStart = Convert.ToInt32(args[1]);
                int iAddressEnd = Convert.ToInt32(args[2]);
                int iSubnetStart = Convert.ToInt32(args[3]);
                int iSubnetEnd = iSubnetStart + Convert.ToInt32(args[4]);
                bool isFileEnabled = Convert.ToBoolean(args[5]);
                bool isConnectionFailed = !oDatabase.checkConnection();
                XmlDocument oDocument = new XmlDocument();
                XmlNode oDocumentHeader = oDocument.CreateXmlDeclaration("1.0", "UTF-16", null);
                XmlNode oHosts = oDocument.CreateElement("hosts");
                Stopwatch oWatch = new Stopwatch();
                ARPHelper oHelper = new ARPHelper(oDocument, oHosts, isFileEnabled, oDatabase);

                oDocument.AppendChild(oDocumentHeader);

                if (isConnectionFailed)
                {
                    isFileEnabled = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Scan started at {DateTime.Now}, but no DB connection!\nCreating XML file instead");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write($"Scan started at {DateTime.Now}");
                }

                oWatch.Start();

                StartProcess(iSubnetStart, iSubnetEnd, iAddressStart, iAddressEnd, ref oHelper, sBaseSubnet);

                oWatch.Stop();

                int iHostCount;

                if (isFileEnabled)
                {
                    iHostCount = SaveToFile(oDocument, oHosts);
                }
                else
                {
                    iHostCount = oDatabase.GetRowCount("computers");

                }

                TimeSpan ts = oWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Scan complete,elapsed time: {elapsedTime}\nTotal hosts: {iHostCount}");
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Usage -> ConsoleScanner.exe [baseSubnet[x.x] addressStart[0-255] addressEnd[0-255] subnetStart[0-255] subnetCount[1->255-subnetStart] file=[true/false]]");

            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static void StartProcess(int subnetStart, int subnetEnd, int addressStart, int addressEnd, ref ARPHelper helper, string baseSubnet)
        {
            List<Thread> oThreads = new List<Thread>();

            for (int x = subnetStart; x < subnetEnd; x++)
            {
                for (int i = addressStart; i < addressEnd; i++)
                {
                    helper.EnqueueIpAddress($"{baseSubnet}.{x}.{i}");
                }

                for (int i = 0; i < addressEnd; i++)
                {
                    oThreads.Add(new Thread(helper.GetMAC));
                    oThreads[i].Start();
                }

                for (int i = 0; i < addressEnd; i++)
                {
                    oThreads[i].Join();
                }

                oThreads.RemoveRange(0, addressEnd);
            }
        }

        private static int SaveToFile(XmlDocument oDocument, XmlNode oHosts)
        {
            oDocument.AppendChild(oHosts);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true
            };

            string filename = $"scan_{DateTime.Now.Ticks}.xml";
            XmlWriter writer = XmlWriter.Create(filename, settings);

            oDocument.Save(writer);
            return oDocument.GetElementsByTagName("host").Count;
        }
    }
}
