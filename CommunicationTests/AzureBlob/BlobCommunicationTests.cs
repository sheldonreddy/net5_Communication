using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SLS.Shared.Communication.AzureBlob;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace SLS.Shared.CommunicationTests.AzureBlob
{
    public class BlobCommunicationTests
    {
        [Fact]
        public void UploadDownloadDeleteTest()
        {
            var settings = new BlobSettings();
            settings.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=storageaccountrouteb72d;AccountKey=Cx25TChprLkAVa70RNloVCuP4r2Md76b6Vmj+ISdisInfHr22H2cRFxNS7+OHlYh4NF3pe3SYUDrh1bu8CxbRw==;EndpointSuffix=core.windows.net";
            IOptions<BlobSettings> options = Options.Create(settings);
            IBlobCommunication comms = new BlobCommunication(options);

            var data = "Test data2";
            var isSuccessful = comms.Upload("testcontainer", "testdata.txt", data).Result;
            Assert.True(isSuccessful);
            var data2 = comms.Download("testcontainer", "testdata.txt").Result;
            Assert.Equal("Test data2", data2);
            var isSuccessful2 = comms.Delete("testcontainer", "testdata.txt").Result;
            Assert.True(isSuccessful2);
        }
    }
}
