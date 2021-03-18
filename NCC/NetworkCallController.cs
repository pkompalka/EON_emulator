using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Utility;

namespace NCC
{
    public class NetworkCallController
    {
        public Directory directory = new Directory();
        public PolicyController policy = new PolicyController();

        // lista z adresami IP i socketami interfejsów służących do słuchania
        public static Dictionary<string, string> InterfaceToListen = new Dictionary<string, string>();

        // lista z adresami IP interfejsów służących do wysyłania
        public static Dictionary<string, string> InterfaceToSend = new Dictionary<string, string>();

        //do identyfikacji directory policy - nazwy hostow
        public string SourceName { get; set; }
        public string DestinationName { get; set; }

        IPAddress DestinationIPAddress;
        IPAddress SourceIPAddress;

        string demandedCapacity { get; set; }

        
        //adres IP_NCC
        public IPAddress NCCIPAddress { get; set; }
        public int NCCPort = 11000; 
        //adres Conection Controlera tego NCC
        public IPAddress CCIPAddress { get; set; }

        //public IPAddress CPCCIPAddress { get; set; }

        //flaga czy hosty są w tej samej domenie
        public bool FlagDomain;

        //numer domeny NCC
        public int Domein;

        //lista sąsiednich NCC w domenie
        public static Dictionary<int,IPAddress> Other_NCC = new Dictionary<int, IPAddress>();

        public NCCLogs myWindow;

        public NetworkCallController(int numberofdomain, NCCLogs tj)
        {
            myWindow = tj;
            ReadConfig(numberofdomain);
            directory.ReadConfig();
            policy.ReadConfig();
        }



        public void ReadConfig(int numberofdomain)
        {
            XmlReader reader = new XmlReader("ConfigurationNCC" + numberofdomain + ".xml");
            NCCIPAddress = IPAddress.Parse(reader.GetAttributeValue("networkcallcontroller", "IP_ADDRESS_NCC"));
            CCIPAddress = IPAddress.Parse(reader.GetAttributeValue("networkcallcontroller", "IP_ADDRESS_CC"));
           

            int numberofncc = reader.GetNumberOfItems("OTHER_NCC");
            for(int i=0; i<numberofncc;i++)
            {
                int tmpdomain = Int32.Parse(reader.GetAttributeValue(i, "OTHER_NCC", "DOMAIN"));
                IPAddress tmp = IPAddress.Parse(reader.GetAttributeValue(i, "OTHER_NCC", "IP_ADDRESS_OTHER_NCC"));
                Other_NCC.Add(tmpdomain,tmp);
            }
        }

        public void ListenForConnections()
        {
   
            byte[] buffer = new byte[64];
            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //socket.Bind(new IPEndPoint(IPAddress.Any,0)); 
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(NCCIPAddress, 11000);
            UdpClient newsock = new UdpClient(ipep);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                NewLog($"Listen for connection", myWindow,"LightSeaGreen");
                Task.Run(() =>
               {
                    while (true)
                    {
                      
                      buffer = newsock.Receive(ref sender);
                      if (buffer.Length > 0)
                      {
                            ReceivedMessage(buffer);
                           //await Task.Delay(100);
                       }
                        buffer = null;
                    }
                });
            }
            catch(Exception e)
            {
                e.GetType();
                NewLog("catch", myWindow,"Yellow");
            }
           

        }

        public void ReceivedMessage(byte[] buffer)
        {
         
            string message = Encoding.ASCII.GetString(buffer);
            string action = message.Split(null)[0];
            var tmp = message.Replace($"{action} ", "");
            var data = tmp.Split(' ');

            //NewLog($"Received Messsage {action} <potrzebny, ew. adres skad dostal?>", myWindow, "Blue");

            switch (action)
            {
                case MessageNames.CALL_REQUEST:
                    CallRequest(data);
                    break;
                case MessageNames.CALL_COORDINATION:
                    CallCoordination(data);
                    break;
                case MessageNames.CALL_CONFIRMED_FROM_CPCC:
                    CallConfirmed(data);
                    break;
                case MessageNames.CALL_CONFIRMED_FROM_NCC:
                    NewLog($"Get Call Confirmed from other NCC", myWindow, "LightSeaGreen");
                    ConnectionRequest(data);
                    break;
                case MessageNames.CALL_CONFIRMED:
                    SendConnectionToHost(data);
                    break;
            }

        }

        public void SendConnectionToHost(string[] data)
        {
            try
            {
                //NewLog($"Send CallConfirmed to: {SourceIPAddress.ToString()}", myWindow, "LightSeaGreen");
                SlotWindow slotWindow = XmlSerialization.DeserializeObject<SlotWindow>(
                    XmlSerialization.GetStringToNormal(data));
                Connections connection = new Connections();
                connection.slotWindow = slotWindow;
                connection.Destination = DestinationIPAddress.ToString();
                string message = MessageNames.ADD_CONNECTION + " " + XmlSerialization.SerializeObject(connection);
                SendMessage(message, SourceIPAddress.ToString(), 11000);
            }
            catch(Exception ex)
            {
                NewLog($"SendConnectionToHost {ex}", myWindow, "AntiqueWhite");
            }
        }

        //Connection request do CC o zestawienie połaczenia action source detination demanded czywtejsamejdomenie
        private void ConnectionRequest(string[] data)
        {
            NewLog($"Send Connection Request to CC: {CCIPAddress} [ {data[0]} Src: {data[1]} Dest: {data[2]} Capacity: {data[3]} ]", myWindow, "LightSeaGreen");
           
            string message = null;
            int domainflag;
            //sprawdzamy czy są w tej samej domenie
            if (directory.Directory_request(IPAddress.Parse(data[1]), IPAddress.Parse(data[2])))
            {
                domainflag = 1;
            }
            else
            {
                domainflag = 0;
            }
            message = MessageNames.CONNECTION_REQUEST + " " + data[1] + " " + data[2] +" "+data[3] +" "+ domainflag.ToString();
            SendMessage(message, CCIPAddress.ToString(), 11000);
        
        }

        

        private void CallCoordination(string[] data)
        {

            NewLog($"Get Call Coordination", myWindow, "LightSeaGreen");
            SourceIPAddress = IPAddress.Parse(data[0]);
            string destinationID = data[1];
            string demandedCapacity = data[2];
            //source adres
            IPAddress NCC_address = IPAddress.Parse(data[3]);
            DestinationIPAddress = directory.Translation_Address(destinationID);
            FlagDomain =  directory.Directory_request(SourceIPAddress,destinationID);
            int domena = directory.ReturnDomainDestinationHost(destinationID);

            
            NewLog($"Directory:  Translation destination address {DestinationIPAddress}", myWindow, "LimeGreen");
            NewLog($"Policy: can use services", myWindow, "LimeGreen");
            if (policy.PolicyAccess(destinationID))
            {
               
                SendCallIndication(SourceIPAddress, DestinationIPAddress, demandedCapacity, NCC_address);     
            }
                
        }

        private void CallRequest(string[] data)
        {
          
            SourceName =data[0];
            DestinationName = data[1];
            demandedCapacity = data[2];
            NewLog($"Get CallRequest from {SourceName}", myWindow, "LightSeaGreen");
          
            FlagDomain =  directory.Directory_request(SourceName,DestinationName);
            int domena = directory.ReturnDomainDestinationHost(DestinationName);
            //przekonwertowane nazwy hostow na ich adresy IP
            SourceIPAddress = directory.Translation_Address(SourceName);
            DestinationIPAddress = directory.Translation_Address(DestinationName);
            

            if (policy.PolicyAccess(DestinationName))
            {
                if(FlagDomain)
                {
                    //call indication
                    NewLog($"Directory: Translation source address: {SourceIPAddress}", myWindow, "LimeGreen");
                    NewLog($"Directory: Translation destination address: {DestinationIPAddress}", myWindow, "LimeGreen");
                    NewLog($"Policy: can use services", myWindow, "LimeGreen");
                    SendCallIndication(SourceIPAddress, DestinationIPAddress, demandedCapacity, NCCIPAddress);
                }else
                {
                    //call coordination
                    NewLog($"Directory:  Translation source address: {SourceIPAddress}", myWindow, "LimeGreen");
                    NewLog($"Directory:  Destination address: {DestinationIPAddress} is not in that domain", myWindow, "LimeGreen");
                    NewLog($"Policy: can use services", myWindow, "LimeGreen");
                    SendCallCoordination(SourceIPAddress, DestinationName, demandedCapacity, NCCIPAddress, domena); 
                }
            }
            else
            {
                NewLog($"Policy: cannot use services",myWindow, "LightSeaGreen");
            }
        }

        //accept + " " + source + " " + destination + " " + demanded+" "+ ncc_source;
        public void CallConfirmed(string[] data)
        {
            string logHostCPCC;
            //if (destination.Equals(IPAddress.Parse("127.0.0.10")))
            if (data[2].Equals("127.0.0.10"))
            {
                logHostCPCC = "127.0.1.1";
            }
            else if (data[2].Equals("127.0.0.20"))
            {
                logHostCPCC = "127.0.1.2";
            }
            else if (data[2].Equals("127.0.0.30"))
            {
                logHostCPCC = "127.0.1.3";
            }
            else
            {
                logHostCPCC = "cpcc";
            }
            //NewLog($"{NCCIPAddress} CALL CONFIRMED: {CCIPAddress} [{data[0]} {data[1]} {data[2]}]", myWindow, "LightSeaGreen");
            NewLog($"Get CallConfirmed from CPCC: {logHostCPCC} [ {data[0]} Src: {data[1]} Dest: {data[2]} Capacity: {data[3]} ]", myWindow, "LightSeaGreen");

            IPAddress address = IPAddress.Parse(data[4]);
            if(NCCIPAddress.Equals(address))
            {
                ConnectionRequest(data);
            }else
            {
                NewLog($"Send Call Confirmed to NCC {address}", myWindow, "LightSeaGreen");
                CallConfirmedNCC(data);
            }
        }

        private void CallConfirmedNCC(string[] data)
        {
             SendCallConfirmedNCC(data);
        }

        //wiadomosc do kolejnego ncc
        public void SendCallCoordination(IPAddress source, string destinationID, string demandedCapacity, IPAddress ncc_address, int domena)
        {
            string message = null;
            message = MessageNames.CALL_COORDINATION + " "+source.ToString() + " "+destinationID + " " + demandedCapacity + " "+ ncc_address.ToString();
            IPAddress ip;
            Other_NCC.TryGetValue(domena, out ip);
            NewLog($"Send Call Coordination to other NCC {ip.ToString()} ", myWindow, "LightSeaGreen");

            SendMessage(message,ip.ToString(),11000);
        }

        //po uwierzytelnieniu wysylam CallIndication do CPCC destination, STATIC
        public void SendCallIndication(IPAddress source , IPAddress destination , string demandedCapacity , IPAddress ncc_address)
        {
            string logHostCPCC;           
            if (destination.Equals(IPAddress.Parse("127.0.0.10")))
            {
                logHostCPCC = "127.0.1.1";
            }
            else if (destination.Equals(IPAddress.Parse("127.0.0.20")))
            {
                logHostCPCC = "127.0.1.2";
            }
            else if (destination.Equals(IPAddress.Parse("127.0.0.30")))
            {
                logHostCPCC = "127.0.1.3";
            }
            else
            {
                logHostCPCC = "cpcc";
            }
            NewLog($"Send Call Indication Source: {source} Destination: {destination} DemandedCapacity: {demandedCapacity} to CPCC: {logHostCPCC}", myWindow, "LightSeaGreen");
            string message = null;
            message = MessageNames.CALL_INDICATION + " " + source.ToString() + " " + destination.ToString() + " " + demandedCapacity + " " + ncc_address.ToString();
            SendMessage(message,destination.ToString(),11000);
        }

        public void SendCallConfirmedNCC(string[] data) 
        {
            string message = null;
            message = MessageNames.CALL_CONFIRMED_FROM_NCC +" "+data[0]+" "+data[1] +" "+data[2] + " " + data[3]+" "+data[4];
            string ip_ncc = data[4];
            SendMessage(message, ip_ncc,11000);
        }

        //metoda wysylajaca gotowa wiadomosc w bitach 
        public static void SendMessage(string message, string ip, int port)
        {
            byte[] messagebyte = new byte[message.Length];
            messagebyte = Encoding.ASCII.GetBytes(message);
            UdpClient client = new UdpClient();
            client.Send(messagebyte, messagebyte.Length,ip, port);
        }

        public void NewLog(string info, NCCLogs tmp, string color)
        { 
            myWindow.Dispatcher.Invoke(() => myWindow.NewLog(info, tmp,color));

        }
    }
}
