using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SLS.Shared.Communication.TCP
{
    public abstract class TcpCommunicationBase
    {
        protected static ConcurrentDictionary<Guid, TcpDataObject> InputBuffer = new ConcurrentDictionary<Guid, TcpDataObject>();
        protected static ConcurrentDictionary<Guid, TcpDataObject> OutputBuffer = new ConcurrentDictionary<Guid, TcpDataObject>();
        protected static ConcurrentDictionary<Guid, TcpConnectionObject> ConnectionBuffer = new ConcurrentDictionary<Guid, TcpConnectionObject>();

        public abstract void ProcessIncoming(Guid clientId, TcpClient client, CancellationToken stoppingToken);
        public abstract void ProcessOutgoing(Guid clientId, TcpClient client, CancellationToken stoppingToken);

        public bool Write(Guid clientId, string data)
        {
            try
            {
                var outputData = new TcpCommsObject(TcpCommsObjectTypes.Data, data).Serialise();
                return AddOutput(new TcpDataObject(clientId, outputData));
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool Disconnect(Guid clientId)
        {
            try
            {
                var outputData = new TcpCommsObject(TcpCommsObjectTypes.Disconnect, "Request").Serialise();
                return AddOutput(new TcpDataObject(clientId, outputData));
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ReadNext(Guid clientId, out string data)
        {
            data = null;
            try
            {
                if (InputBuffer.Any(i => i.Value.ClientId == clientId))
                {
                    var id = InputBuffer.Where(i => i.Value.ClientId == clientId).First().Key;
                    var success = GetInput(clientId, out var tcpData) && RemoveInput(clientId);
                    data = tcpData.Data;
                    return success;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ReadNextAny(out Guid clientId, out string data)
        {
            clientId = Guid.Empty;
            data = null;
            try
            {
                var success = true;
                success &= GetNextInput(out var id, out var tcpDataObj);
                data = tcpDataObj.Data;
                clientId = tcpDataObj.ClientId;
                success &= RemoveInput(id);
                return success;

            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UpdateHeartbeat(Guid id)
        {
            try
            {
                ConnectionBuffer[id] = ConnectionBuffer[id].UpdateHeartbeat();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool AddConnection(Guid id, TcpConnectionObject connectionObj)
        {
            try
            {
                return ConnectionBuffer.TryAdd(id, connectionObj);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool DisconnectConnection(Guid id)
        {
            try
            {
                ConnectionBuffer[id] = ConnectionBuffer[id].DisconnectConnection();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool RemoveConnection(Guid id)
        {
            try
            {
                return ConnectionBuffer.TryRemove(id, out _);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool HasConnection(Guid id)
        {
            try
            {
                return ConnectionBuffer.TryGetValue(id, out _);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool AddInput(TcpDataObject dataObj)
        {
            try
            {
                return InputBuffer.TryAdd(Guid.NewGuid(), dataObj);

            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool GetInput(Guid id, out TcpDataObject dataObj)
        {
            dataObj = new TcpDataObject();
            try
            {
                return InputBuffer.TryGetValue(id, out dataObj);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool RemoveInput(Guid id)
        {
            try
            {
                return InputBuffer.TryRemove(id, out _);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool GetNextInput(out Guid id, out TcpDataObject dataObj)
        {
            id = Guid.Empty;
            dataObj = new TcpDataObject();
            try
            {
                if (InputBuffer.Any())
                {
                    id = InputBuffer.First().Key;
                    return GetInput(id, out dataObj);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool AddOutput(TcpDataObject dataObj)
        {
            try
            {
                return OutputBuffer.TryAdd(Guid.NewGuid(), dataObj);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool GetOutput(Guid id, out TcpDataObject dataObj)
        {
            dataObj = new TcpDataObject();
            try
            {
                return OutputBuffer.TryGetValue(id, out dataObj);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool RemoveOutput(Guid id)
        {
            try
            {
                return OutputBuffer.TryRemove(id, out _);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool GetNextOutput(Guid clientId, out Guid id, out TcpDataObject dataObj)
        {
            id = Guid.Empty;
            dataObj = new TcpDataObject();
            try
            {
                if (OutputBuffer.Any(b => b.Value.ClientId == clientId))
                {
                    id = OutputBuffer.Where(b => b.Value.ClientId == clientId).First().Key;
                    return GetOutput(id, out dataObj);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
  
    }
}
