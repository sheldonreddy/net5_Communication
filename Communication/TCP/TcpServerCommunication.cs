using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace SLS.Shared.Communication.TCP
{

    public interface ITcpServerCommunication
    {
        void StartListener(CancellationToken stoppingToken);
        bool ReadNext(Guid clientId, out string data);
        bool ReadNextAny(out Guid clientId, out string data);
        bool Write(Guid clientId, string data);
    }

    public class TcpServerCommunication : TcpCommunicationBase, ITcpServerCommunication
    {
        private IPEndPoint EndPoint;
        private readonly TcpListener Server;
        private readonly ILogger<TcpServerCommunication> Logger;
        private readonly IOptions<TcpServerSettings> Settings;

        public TcpServerCommunication(ILogger<TcpServerCommunication> logger, IOptions<TcpServerSettings> settings)
        {
            Logger = logger;
            Settings = settings;

            //just for testing
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            EndPoint = new IPEndPoint(IPAddress.Any, Settings.Value.Port);
            Server = new TcpListener(EndPoint);
        }

        public void StartListener(CancellationToken stoppingToken)
        {
            try
            {              
                Server.Start();
                Logger.LogInformation($"Tcp listener --> started with IP {EndPoint.Address}, Port {EndPoint.Port}.");
                Task.Run(() => ConnectionManager(stoppingToken), stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = Server.AcceptTcpClient();                  
                    var clientId = Guid.NewGuid();
                    Logger.LogInformation($"Tcp listener --> connected to new client {clientId}.");

                    var connectionObj = new TcpConnectionObject(new CancellationTokenSource());
                    AddConnection(clientId, connectionObj);

                    var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(connectionObj.TokenSource.Token, stoppingToken);
                    Task.Run(() => ProcessIncoming(clientId, client, linkedTokenSource.Token), linkedTokenSource.Token);
                    Task.Run(() => ProcessOutgoing(clientId, client, linkedTokenSource.Token), linkedTokenSource.Token);
                }
                Logger.LogInformation($"Tcp listener --> stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Tcp listener --> fatal error occurred: \n{ex}.");
                Server.Stop();
            }
        }
        public void ConnectionManager(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogDebug($"Connection manager --> started.");
                var timer = new System.Timers.Timer();
                timer.Elapsed += (source, e) =>
                {
                    timer.Stop();
                    try
                    {
                        for (int i = ConnectionBuffer.Count - 1; i >= 0; i--)
                        {
                            if (ConnectionBuffer.ElementAt(i).Value.Disconnect || (DateTime.Now - ConnectionBuffer.ElementAt(i).Value.Heartbeat).TotalSeconds > Settings.Value.HeartbeatTimeout)
                            {
                                var id = ConnectionBuffer.ElementAt(i).Key;
                                string condition = ConnectionBuffer.ElementAt(i).Value.Disconnect ? "disconnect" : "heartbeat timeout";
                                Logger.LogDebug($"Connection manager --> connection {id} found with {condition} condition.");
                                ConnectionBuffer.ElementAt(i).Value.TokenSource.Cancel();
                                Logger.LogDebug($"Connection manager --> cancelled token source for connection {id}.");
                                RemoveConnection(id);
                                Logger.LogDebug($"Connection manager --> removed connection {id}.");
                                Logger.LogInformation($"Connection manager --> closed and removed connection {id} due to {condition} condition.");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.LogError($"Connection manager --> error occurred: {ex}");
                    }
                    timer.Start();
                };
                timer.Interval = Settings.Value.ConnectionManagerInterval;
                timer.Start();
                stoppingToken.WaitHandle.WaitOne();
                timer.Stop();
                Logger.LogDebug($"Connection manager --> stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Connection manager --> fatal error occurred: {ex}");
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
                    if (client.Connected && stream.DataAvailable)
                    {
                        try
                        {
                            Logger.LogDebug($"Client {clientId} --> stream data available.");
                            byte[] input = new byte[Settings.Value.ByteArraySize];
                            var i = stream.Read(input, 0, input.Length);
                            var data = Encoding.ASCII.GetString(input, 0, i);
                            Logger.LogDebug($"Client {clientId} --> read and converted data:\n{data}.");

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
                                        Logger.LogDebug($"Client {clientId} --> received heartbeat.");
                                        var heartbeatUpdate = UpdateHeartbeat(clientId);
                                        Logger.LogDebug($"Client {clientId} --> heartbeat updated in connection buffer({(heartbeatUpdate ? "success" : "fail")}).");
                                        var outputAdd1 = AddOutput(new TcpDataObject(clientId, new TcpCommsObject(TcpCommsObjectTypes.Heartbeat, "Received.").Serialise()));
                                        Logger.LogDebug($"Client {clientId} --> added heartbeat to output buffer ({(outputAdd1 ? "success" : "fail")}).");
                                        break;
                                    case TcpCommsObjectTypes.Data:
                                        Logger.LogDebug($"Client {clientId} --> received data.");
                                        var inputAdd = AddInput(new TcpDataObject(clientId, commsObj.Data));
                                        Logger.LogDebug($"Client {clientId} --> added data to input buffer ({(inputAdd ? "success" : "fail")}).");
                                        Logger.LogInformation($"Client {clientId} --> received and added data to input buffer ({(inputAdd ? "success" : "fail")}).");
                                        break;
                                    case TcpCommsObjectTypes.Disconnect:
                                        Logger.LogDebug($"Client {clientId} --> received diconnect request.");
                                        var outputAdd2 = AddOutput(new TcpDataObject(clientId, new TcpCommsObject(TcpCommsObjectTypes.Disconnect, "Received.").Serialise()));
                                        Logger.LogDebug($"Client {clientId} --> added disconnect response to output buffer ({(outputAdd2 ? "success" : "fail")}).");
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Client {clientId} --> error occurred processing incoming data: {ex}");
                        }
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
                            Logger.LogDebug($"Client {clientId} --> output data available:\n{outputDataObj.Data}");
                            var commsObj = new TcpCommsObject(outputDataObj.Data);
                            byte[] output = Encoding.ASCII.GetBytes(outputDataObj.Data);
                            switch (commsObj.Type)
                            {
                                case TcpCommsObjectTypes.Heartbeat:
                                    stream.Write(output, 0, output.Length);
                                    Logger.LogDebug($"Client {clientId} --> sent heartbeat response.");
                                    var outputRemove1 = RemoveOutput(outputId);
                                    Logger.LogDebug($"Client {clientId} --> removed heartbeat from output buffer ({(outputRemove1 ? "success" : "fail")}).");
                                    break;
                                case TcpCommsObjectTypes.Data:
                                    stream.Write(output, 0, output.Length);
                                    Logger.LogInformation($"Client {clientId} --> sent data {outputId}.");
                                    var outputRemove2 = RemoveOutput(outputId);
                                    Logger.LogDebug($"Client {clientId} --> removed data ({outputId}) from output buffer ({(outputRemove2 ? "success" : "fail")}).");
                                    break;
                                case TcpCommsObjectTypes.Disconnect:
                                    stream.Write(output, 0, output.Length);
                                    Logger.LogDebug($"Client {clientId} --> sent disconnect response.");
                                    var connectionDisconnect1 = DisconnectConnection(clientId);
                                    Logger.LogDebug($"Client {clientId} --> disconnect status updated in connection buffer ({(connectionDisconnect1 ? "success" : "fail")}).");
                                    var outputRemove3 = RemoveOutput(outputId);
                                    Logger.LogDebug($"Client {clientId} --> removed disconnect response from output buffer ({(outputRemove3 ? "success" : "fail")}).");
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
