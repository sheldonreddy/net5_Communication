using System;
using System.Collections.Generic;
using System.Text;

namespace SLS.Shared.Communication.TCP
{
    public enum TcpCommsObjectTypes : int
    {
        Heartbeat = 0,
        Disconnect = 1,
        Data = 2,
        Invalid = 3
    }
}
