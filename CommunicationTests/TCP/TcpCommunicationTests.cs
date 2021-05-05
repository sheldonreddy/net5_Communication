using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using SLS.Shared.Communication.TCP;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading;

namespace SLS.Shared.CommunicationTests.TCP
{
    public class TcpCommunicationTests
    {
        [Fact]
        public void ClientTest()
        {
            //var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            //var factory = serviceProvider.GetService<ILoggerFactory>();
            //var logger = factory.CreateLogger<TcpClientCommunication>();
            //IOptions<TcpClientSettings> options = Options.Create(new TcpClientSettings());

            //ITcpClientCommunication comms = new TcpClientCommunication(logger, options);
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            //comms.StartClient(ipAddress, 11000, out var clientId);
            //Thread.Sleep(100000);
        }
    }
}
