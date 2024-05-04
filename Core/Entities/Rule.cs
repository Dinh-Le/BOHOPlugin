using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BOHO.Core.Entities
{
    public class Rule
    {
        public class LoiteringMetadata
        {
            [JsonProperty("time_stand")]
            public string TimeStand { get; set; }
        }

        public class TripwireMetadata
        {
            [JsonProperty("direction")]
            public string Direction { get; set; }
        }

        public class AlarmMetadata
        {
            [JsonProperty("loitering")]
            public LoiteringMetadata Loitering { get; set; }

            [JsonProperty("tripwire")]
            public TripwireMetadata Tripwire { get; set; }
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("alarm_type")]
        public string AlarmType { get; set; }

        [JsonProperty("points")]
        public int[][] Points { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("preset_id")]
        public int PresetID { get; set; }

        [JsonProperty("schedule_id")]
        public int ScheduleID { get; set; }

        [JsonProperty("objects")]
        public string[] Objects { get; set; }
    }
}
