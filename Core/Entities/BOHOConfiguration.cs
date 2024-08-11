namespace BOHO.Core.Entities
{
    public class BOHOConfiguration
    {
        public string IP { get; set; }
        public int ApiPort { get; set; }
        public int WebPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int MilestoneId { get; set; }
        public string MqttTopic { get; set; }
        public string MqttHost { get; set; }
        public int MqttPort { get; set; }
        public int AnalyticImageWidth { get; set; }
        public int AnalyticImageHeight { get; set; }
    }
}
