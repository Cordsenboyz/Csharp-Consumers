using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PowerConsumer
{
    internal class Data
    {
        [JsonProperty("house_id")]
        public int HouseId { get; set; }
        public int Timestamp { get; set; }
        public double Kwh { get; set; }
    }
}
