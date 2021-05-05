using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SLS.Shared.Communication.TCP
{
    public class TcpServerSettings
    {
        public IPAddress IPAdress { get; set; }
        public int Port { get; set; }

        public int ConnectionManagerInterval { get; set; }
        public int ProcessIncomingInterval { get; set; }
        public int ProcessOutgoingInterval { get; set; }
        
        public int HeartbeatTimeout { get; set; }
        public int ByteArraySize { get; set; }
    }
}
