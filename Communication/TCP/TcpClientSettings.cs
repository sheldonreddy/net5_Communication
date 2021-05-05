namespace SLS.Shared.Communication.TCP
{
    public class TcpClientSettings
    {
        public int HeartbeatInterval { get; set; }
        public int ConnectionInterval { get; set; }
        public int ProcessIncomingInterval { get; set; }
        public int ProcessOutgoingInterval { get; set; }
        public int HeartbeatTimeout { get; set; }
        
        public int ByteArraySize { get; set; }
    }
}