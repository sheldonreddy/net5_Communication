using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace SLS.Shared.CommunicationTests.ADF
{
    public class DurableFunctionCommunicationTests
    {
        [Fact]
        public void DurableFunctionStartReqTest()
        {
            ////Uri url = new Uri("https://vrpschedulingengine.azurewebsites.net/api/HttpStart");
            //////Uri url = new Uri("http://localhost:7071/api/HttpStart");
            //var scheduleData = System.IO.File.ReadAllText(@"C:\Users\Chetan PC\Desktop\pepsi_test_3.txt");

            //var obj = ObjectTranslation.DeserialiseScheduleInputs(scheduleData);
            //obj.SolveOptions.InitialStrategy = RoutingObjects.Models.ScheduleEngine.InitialStrategy.Savings;
            //obj.SolveOptions.Metaheuristic = RoutingObjects.Models.ScheduleEngine.Metaheuristic.Automatic;
            ////obj.SolveOptions.TimeLimit = 600;
            //obj.SolveOptions.MaximumMemoryUsageBytes = 2000000000;
            //obj.SolveOptions.TimeLimit = 1800;
            //var jsonData = ObjectTranslation.Serialise(obj);


            ////string mediaType = "application/json";
            ////string jsonData = "{\"Locations\":[{\"lat\":-26.165,\"lon\":28.191},{\"lat\":-26.1761,\"lon\":27.9678},{\"lat\":-26.151693,\"lon\":28.171045}],\"Country\":\"SouthAfrica\",\"Priority\":\"High\"}";


            ////comms.SetApiAuthentication("default", "hMkaVxFQ/e1kK0WqBRdNnAP77ij0hspOReijW1Aic6P7v/a5kmrfGA==");
            ////comms.SetMediaType(mediaType);
            //for (int i = 0; i < 5; i++)
            //{
            //    DurableFunctionCommunication comms = new DurableFunctionCommunication(url);
            //    comms.InitiateHttpStartReq(jsonData, 10);
            //    comms.GetStatusQueryResponse(5, 10);
            //    Thread.Sleep(1000);
            //}
        }

    }
}
