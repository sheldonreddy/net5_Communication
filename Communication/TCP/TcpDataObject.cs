using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SLS.Shared.Communication.TCP
{
    public class TcpDataObject
    {
        public Guid ClientId = Guid.Empty;
        public string Data = null;

        public TcpDataObject() { }
        public TcpDataObject(Guid clientId, string data)
        {
            ClientId = clientId;
            Data = data;
        }
    }
}
