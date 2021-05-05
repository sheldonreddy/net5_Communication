using System;

namespace SLS.Shared.Communication.ADF
{
    public class DurableFunctionHttpResponse
    {
        public string id;
        public Uri statusQueryGetUri;
        public Uri sendEventPostUri;
        public Uri terminatePostUri;
        public Uri purgeHistoryDeleteUri;
    }
}
