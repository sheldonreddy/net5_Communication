using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SLS.Shared.Communication.TCP
{
    public class TcpConnectionObject
    {
        public bool Disconnect = false;
        public DateTime Heartbeat;
        public CancellationTokenSource TokenSource;

        public TcpConnectionObject(CancellationTokenSource tokenSource)
        {
            Disconnect = false;
            Heartbeat = DateTime.Now;
            TokenSource = tokenSource;
        }

        public TcpConnectionObject UpdateHeartbeat()
        {
            Heartbeat = DateTime.Now;
            return this;
        }

        public TcpConnectionObject DisconnectConnection()
        {
            Disconnect = true;
            return this;
        }
    }
}
