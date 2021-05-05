using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SLS.Shared.ActivityLog;
using System.Threading;

namespace SLS.Shared.Communication.ADF
{
    public class DurableFunctionCommunication
    {
        
        public Status HttpStartReqStatus { get; private set; } = Status.Undefined;
        public Status StatusQueryRespStatus { get; private set; } = Status.Undefined;
        public DurableFunctionHttpResponse HttpResponse { get; private set; } = new DurableFunctionHttpResponse();
        public DurableFunctionStatusQueryResponse StatusQueryResponse { get; private set; } = new DurableFunctionStatusQueryResponse();
        
        private readonly Uri BaseUrl = null;
        private Tuple<string, string> ApiAuthentication = null;
        private MediaTypeWithQualityHeaderValue MediaType = new MediaTypeWithQualityHeaderValue("application/json");

        public DurableFunctionCommunication(Uri baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public void SetApiAuthentication(string apiKey, string apiValue)
        {
            ApiAuthentication = new Tuple<string,string>(apiKey, apiValue);
        }

        public void SetMediaType(string mediaType)
        {
            MediaType = new MediaTypeWithQualityHeaderValue(mediaType);
        }

        public void InitiateHttpStartReq(string jsonData, int maxRetries = 0)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    HttpStartReqStatus = Status.Initialise;
                    client.BaseAddress = BaseUrl;
                    if (ApiAuthentication != null)
                        client.DefaultRequestHeaders.Add(ApiAuthentication.Item1, ApiAuthentication.Item2);
                    if (MediaType != null)
                        client.DefaultRequestHeaders.Accept.Add(MediaType);

                    var content = new StringContent(jsonData, Encoding.UTF8, MediaType.MediaType);
                    var retries = 0;
                    HttpStartReqStatus = Status.Pending;

                    while (HttpStartReqStatus == Status.Pending)
                    {
                        var result = client.PostAsync(BaseUrl, content).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            try
                            {
                                string data = result.Content.ReadAsStringAsync().Result;
                                HttpResponse = JsonConvert.DeserializeObject<DurableFunctionHttpResponse>(data);
                                HttpStartReqStatus = Status.Good;
                            }
                            catch (Exception)
                            {
                                HttpStartReqStatus = Status.Bad_DeserializationError;
                            }
                        }
                        else if (retries >= maxRetries)
                            HttpStartReqStatus = Status.Bad_CommunicationError;
                        else
                            retries++;
                    }

                }
                catch(Exception)
                {
                    HttpStartReqStatus = Status.Bad_Exception;
                }
            }
        }

        public void GetStatusQueryResponse(int pollInterval = 30, int maxRetries = 1, bool showInput = false)
        {
            if (HttpStartReqStatus == Status.Good)
            {
                StatusQueryRespStatus = Status.Initialise;
                var worker = System.Threading.Tasks.Task.Factory.StartNew(() => PollStatusQueryResponse(pollInterval, showInput, maxRetries));
                while (StatusQueryRespStatus == Status.Initialise || StatusQueryRespStatus == Status.Pending)
                    Thread.Sleep(pollInterval * 1000);

            }
        }

        private void PollStatusQueryResponse(int interval, bool showInput, int maxRetries)
        {
            var isComplete = false;
            using (var client = new HttpClient())
            {
                try
                {
                    StatusQueryRespStatus = Status.Pending;
                    int retries = 0;
                    while (!isComplete)
                    {
                        string reqUri = showInput ? HttpResponse.statusQueryGetUri.AbsoluteUri : HttpResponse.statusQueryGetUri.AbsoluteUri + "&showInput=false";
                        var result = client.GetAsync(reqUri).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            try
                            {
                                string data = result.Content.ReadAsStringAsync().Result;
                                StatusQueryResponse = JsonConvert.DeserializeObject<DurableFunctionStatusQueryResponse>(data);

                                if (StatusQueryResponse.runtimeStatus == "Completed" || StatusQueryResponse.runtimeStatus == "Failed")
                                {
                                    StatusQueryRespStatus = Status.Good;
                                    isComplete = true;
                                }
                                else
                                {
                                    retries = 0;
                                    Thread.Sleep(interval * 1000);
                                }
                            }
                            catch (Exception)
                            {
                                StatusQueryRespStatus = Status.Bad_DeserializationError;
                                isComplete = true;
                            }
                        }
                        else if (result.StatusCode.ToString() == "TooManyRequests")
                            Thread.Sleep(10000);
                        else if (retries >= maxRetries)
                        {
                            StatusQueryRespStatus = Status.Bad_CommunicationError;
                            isComplete = true;
                        }
                        else
                        {
                            retries++;
                            Thread.Sleep(5000);
                        }
                    }      
                }
                catch (Exception)
                {
                    StatusQueryRespStatus = Status.Bad_Exception;
                    isComplete = true;
                }
            }
        }
    }

}       
