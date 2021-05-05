using System;
using System.Collections.Generic;
using System.Text;

namespace SLS.Shared.Communication.ADF
{
    public class DurableFunctionStatusQueryResponse
    {
        public string name;
        public string instanceId;
        public string runtimeStatus;
        public string input;
        public string customStatus;
        public string output;
        public DateTime createdTime;
        public DateTime lastUpdatedTime;
    }
}
