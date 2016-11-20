using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoBank_TH.Models
{
    public class ServiceRequest
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        [JsonProperty(PropertyName = "UserId")]
        public string UserId { get; set; }
        [JsonProperty(PropertyName = "RequestTitle")]
        public string RequestTitle { get; set; }
        [JsonProperty(PropertyName = "RequestDescription")]
        public string RequestDescription { get; set; }
        [JsonProperty(PropertyName = "ServiceType")]
        public string ServiceType { get; set; }
        [JsonProperty(PropertyName = "AppointmentTime")]
        public string AppointmentTime { get; set; }

    }
}