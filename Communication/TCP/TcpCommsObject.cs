using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SLS.Shared.Communication.TCP
{
    public class TcpCommsObject
    {
        public TcpCommsObjectTypes Type = TcpCommsObjectTypes.Invalid;
        public string Data;

        public TcpCommsObject() { }
        public TcpCommsObject(TcpCommsObjectTypes type, string data)
        {
            Type = type;
            Data = data;
        }
        public TcpCommsObject(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<TcpCommsObject>(json);
                Type = obj.Type;
                Data = obj.Data;
            }
            catch (Exception) { }
        }

        public string Serialise()
        {
            try
            {
                return JsonConvert.SerializeObject(this);
            }
            catch (Exception) { return null; }
        }
    }
}
