using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SLS.Shared.Communication.TCP
{
    public interface ITcpClientCommunication
    {
        void StartClient(IPAddress ipAddress, int port, out Guid clientId);
        bool Write(Guid clientId, string data);
        bool Disconnect(Guid clientId);
        bool ReadNext(Guid clientId, out string data);
        bool ReadNextAny(out Guid clientId, out string data);
    }
    public class TcpClientCommunication : TcpCommunicationBase, ITcpClientCommunication
    {
        private readonly ILogger<TcpClientCommunication> Logger;
        private readonly IOptions<TcpClientSettings> Settings;

        public TcpClientCommunication(ILogger<TcpClientCommunication> logger, IOptions<TcpClientSettings> settings)
        {
            Logger = logger;
            Settings = settings;  
        }

        public void StartClient(IPAddress ipAddress, int port, out Guid clientId)
        {
            clientId = Guid.Empty;
            try
            {
                Logger.LogDebug($"Client starter --> starting new connection with IPAddress:{ipAddress} and port: {port}");
                var newClientId = Guid.NewGuid();
                var client = new TcpClient();
                client.Connect(ipAddress, port);
                var connectionObj = new TcpConnectionObject(new CancellationTokenSource());
                AddConnection(newClientId, connectionObj);
                Logger.LogDebug($"Client starter --> connected to server and created connection object {newClientId}");
                Task.Run(() => Heartbeat(newClientId, connectionObj.TokenSource.Token), connectionObj.TokenSource.Token);
                Task.Run(() => ProcessIncoming(newClientId, client, connectionObj.TokenSource.Token), connectionObj.TokenSource.Token);
                Task.Run(() => ProcessOutgoing(newClientId, client, connectionObj.TokenSource.Token), connectionObj.TokenSource.Token);
                Task.Run(() => Connection(newClientId, connectionObj.TokenSource.Token), connectionObj.TokenSource.Token);               
                Logger.LogDebug($"Client starter --> started processing threads for connection {newClientId}");
                Logger.LogInformation($"Client starter --> successfully connected to server (ipAddress:{ipAddress}, port: {port}) with clientId: {newClientId}.");
                clientId = newClientId;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Client starter --> fatal error occurred in starting client: {ex}.");
            }

        }
        public void Heartbeat(Guid clientId, CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogDebug($"Heartbeat --> started {clientId}");
                var timer = new System.Timers.Timer();
                timer.Elapsed += (source, e) =>
                {
                    timer.Stop();
                    try
                    {
                        var heartbeat = new TcpCommsObject(TcpCommsObjectTypes.Heartbeat, "Request.");
                        Logger.LogDebug($"Heartbeat --> created {clientId} heartbeat request.");
                        AddOutput(new TcpDataObject(clientId, heartbeat.Serialise()));
                        Logger.LogDebug($"Heartbeat --> added {clientId} heartbeat request to output buffer.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Heartbeat --> error occurred creating or adding {clientId} heartbeat request: {ex}");
                    }
                    timer.Start();
                };
                timer.Interval = Settings.Value.HeartbeatInterval;
                timer.Start();
                stoppingToken.WaitHandle.WaitOne();
                timer.Stop();
                Logger.LogDebug($"Heartbeat --> stopped {clientId} heartbeat.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Heartbeat --> fatal error occurred managing {clientId} heartbeat: {ex}");
            }
        }
        public void Connection(Guid clientId, CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogDebug($"Connection {clientId} --> started.");
                var timer = new System.Timers.Timer();
                timer.Elapsed += (source, e) =>
                {
                    timer.Stop();
                    try
                    {                      
                        if (HasConnection(clientId) && (ConnectionBuffer[clientId].Disconnect || 
                        (DateTime.Now - ConnectionBuffer[clientId].Heartbeat).TotalMilliseconds > Settings.Value.HeartbeatTimeout))
                        {
                            var condition = ConnectionBuffer[clientId].Disconnect ? "disconnect" : "heartbeat timeout";
                            Logger.LogDebug($"Connection {clientId} --> has a {condition} condition.");
                            ConnectionBuffer[clientId].TokenSource.Cancel();
                            Logger.LogDebug($"Connection {clientId} --> cancelled token source.");
                            RemoveConnection(clientId);
                            Logger.LogDebug($"Connection {clientId} --> removed connection.");
                            Logger.LogInformation($"Connection {clientId} --> closed and removed connection due to {condition} condition.");
                        }                       
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Connection {clientId} --> error occurred: {ex}.");
                    }
                    timer.Start();
                };
                timer.Interval = Settings.Value.ConnectionInterval;
                timer.Start();
                stoppingToken.WaitHandle.WaitOne();
                timer.Stop();
                Logger.LogDebug($"Connection {clientId} --> stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Connection {clientId} --> fatal error occurred: {ex}.");
            }
        }

        public override void ProcessIncoming(Guid clientId, TcpClient client, CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogDebug($"Client {clientId} --> started incoming process.");
                var stream = client.GetStream();
                Logger.LogDebug($"Client {clientId} --> connected to client stream.");
                var timer = new System.Timers.Timer();
                timer.Elapsed += (source, e) =>
                {
                    timer.Stop();
                    try
                    {
                        if (client.Connected && stream.DataAvailable)
                        {
                            Logger.LogDebug($"Client {clientId} --> stream data available.");
                            byte[] input = new byte[Settings.Value.ByteArraySize];
                            var i = stream.Read(input, 0, input.Length);
                            var data = Encoding.ASCII.GetString(input, 0, i);
                            Logger.LogDebug($"Client {clientId} --> read and converted data:\n{data}");
                            var commsObjects = new List<TcpCommsObject>();
                            JsonTextReader reader = new JsonTextReader(new StringReader(data))
                            {
                                SupportMultipleContent = true
                            };
                            while (true)
                            {
                                if (!reader.Read())
                                    break;
                                JsonSerializer serializer = new JsonSerializer();
                                var commsObj = serializer.Deserialize<TcpCommsObject>(reader);
                                commsObjects.Add(commsObj);
                            }
                            foreach (var commsObj in commsObjects)
                            {
                                switch (commsObj.Type)
                                {
                                    case TcpCommsObjectTypes.Heartbeat:
                                        Logger.LogDebug($"Client {clientId} --> received heartbeat response.");
                                        var heartbeatUpdate = UpdateHeartbeat(clientId);
                                        Logger.LogDebug($"Client {clientId} --> heartbeat updated in connection buffer({(heartbeatUpdate ? "success" : "fail")}).");
                                        break;
                                    case TcpCommsObjectTypes.Data:
                                        Logger.LogDebug($"Client {clientId} --> received data.");
                                        var inputAdd = AddInput(new TcpDataObject(clientId, commsObj.Data));
                                        Logger.LogDebug($"Client {clientId} --> added data to input buffer ({(inputAdd ? "success" : "fail")}).");
                                        Logger.LogInformation($"Client {clientId} --> received and added data to input buffer ({(inputAdd ? "success" : "fail")}).");
                                        break;
                                    case TcpCommsObjectTypes.Disconnect:
                                        Logger.LogDebug($"Client {clientId} --> received diconnect response.");
                                        var connDisconnect = DisconnectConnection(clientId);
                                        Logger.LogDebug($"Client {clientId} --> disconnected client connection ({(connDisconnect ? "success" : "fail")}).");
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Client {clientId} --> error occurred processing incoming data: {ex}");
                    }
                    timer.Start();
                };
                timer.Interval = Settings.Value.ProcessIncomingInterval;
                timer.Start();
                stoppingToken.WaitHandle.WaitOne();
                timer.Stop();
                stream.Dispose();
                client.Dispose();
                Logger.LogDebug($"Client {clientId} --> closed incoming process.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Client {clientId} --> fatal error occurred: {ex}");
            }
        }
        public override void ProcessOutgoing(Guid clientId, TcpClient client, CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogDebug($"Client {clientId} --> started outgoing process.");
                var stream = client.GetStream();
                Logger.LogDebug($"Client {clientId} --> connected to client stream.");
                var timer = new System.Timers.Timer();
                timer.Elapsed += (source, e) =>
                {
                    timer.Stop();
                    while (GetNextOutput(clientId, out var outputId, out var outputDataObj) && client.Connected)
                    {
                        try
                        {
                            Logger.LogDebug($"Client {clientId} --> output data available: \n{outputDataObj.Data}");
                            var commsObj = new TcpCommsObject(outputDataObj.Data);
                            byte[] output = Encoding.ASCII.GetBytes(outputDataObj.Data);
                            switch (commsObj.Type)
                            {
                                case TcpCommsObjectTypes.Heartbeat:
                                    stream.Write(output, 0, output.Length);
                                    Logger.LogDebug($"Client {clientId} --> sent heartbeat request.");
                                    var outputRemove1 = RemoveOutput(outputId);
                                    Logger.LogDebug($"Client {clientId} --> removed heartbeat request from output buffer ({(outputRemove1 ? "success" : "fail")}).");
                                    break;
                                case TcpCommsObjectTypes.Data:
                                    stream.Write(output, 0, output.Length);
                                    Logger.LogInformation($"Client {clientId} --> sent data ({outputId}).");
                                    var outputRemove2 = RemoveOutput(outputId);
                                    Logger.LogDebug($"Client {clientId} --> removed data ({outputId}) from output buffer ({(outputRemove2 ? "success" : "fail")}).");
                                    break;
                                case TcpCommsObjectTypes.Disconnect:
                                    stream.Write(output, 0, output.Length);
                                    Logger.LogDebug($"Client {clientId} --> sent disconnect request.");
                                    var outputRemove3 = RemoveOutput(outputId);
                                    Logger.LogDebug($"Client {clientId} --> removed disconnect request from output buffer ({(outputRemove3 ? "success" : "fail")}).");
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Client {clientId} --> error occurred processing output data {outputId}: {ex}");
                        }
                    }
                    timer.Start();
                };
                timer.Interval = Settings.Value.ProcessOutgoingInterval;
                timer.Start();
                stoppingToken.WaitHandle.WaitOne();
                timer.Stop();
                stream.Dispose();
                client.Dispose();
                Logger.LogDebug($"Client {clientId} --> closed outgoing process.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Client {clientId} --> fatal error occurred: {ex}");
            }
        }
    }
}
