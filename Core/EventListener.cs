using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOHO.Core.Entities;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using Newtonsoft.Json;

namespace BOHO.Core
{
    public class EventListener : IDisposable
    {
        private const string MqttTopic = "milestone-communicate";
        private const string MqttHost = "192.168.100.14";
        private const int MqttPort = 1883;
        private const double ImageWidth = 1920;
        private const double ImageHeight = 1080;
        private readonly IManagedMqttClient _mqttClient;
        private bool _isInitialized;

        public event EventHandler<BOHOEventArgs> EventReceived;

        private class EventData
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("camera_id")]
            public int CameraId { get; set; }

            [JsonProperty("camera_name")]
            public string CameraName { get; set; }

            [JsonProperty("labels")]
            public List<string> Labels { get; set; }

            [JsonProperty("det")]
            public double[][] Det { get; set; }

            [JsonProperty("image_path")]
            public string ImagePath { get; set; }

            [JsonProperty("preset_id")]
            public int PresetId { get; set; }
        }

        public EventListener()
        {
            this._mqttClient = new MqttFactory().CreateManagedMqttClient();
            this._mqttClient.ApplicationMessageReceivedAsync +=
                MqttClient_ApplicationMessageReceivedAsync;
        }

        public void Dispose()
        {
            this._mqttClient.ApplicationMessageReceivedAsync -=
                MqttClient_ApplicationMessageReceivedAsync;
            this._mqttClient.Dispose();
        }

        public async Task Initialize()
        {
            if (this._isInitialized)
            {
                throw new InvalidOperationException("The connection has been initialized");
            }

            var topicFilter = new MqttTopicFilterBuilder().WithTopic(MqttTopic).Build();
            await this._mqttClient.SubscribeAsync(new List<MqttTopicFilter> { topicFilter });

            // Setup and start a managed MQTT client.
            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId("MilestonePluginClient-" + Guid.NewGuid().ToString())
                .WithTcpServer(MqttHost, MqttPort)
                .Build();
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions)
                .Build();
            await this._mqttClient.StartAsync(options);

            this._isInitialized = true;
        }

        private Task MqttClient_ApplicationMessageReceivedAsync(
            MqttApplicationMessageReceivedEventArgs arg
        )
        {
            var payloadString = Encoding.UTF8.GetString([.. arg.ApplicationMessage.PayloadSegment]);

            try
            {
                var eventData = JsonConvert.DeserializeObject<EventData>(payloadString);
                BOHOEventArgs args =
                    new()
                    {
                        DeviceId = eventData.CameraId,
                        DeviceName = eventData.CameraName,
                        PresetId = eventData.PresetId,
                        BoundingBoxes = eventData
                            .Det.Select(det =>
                            {
                                double x = det[0] / ImageWidth;
                                double y = det[1] / ImageHeight;
                                var width = (det[2] - det[0]) / ImageWidth;
                                var height = (det[3] - det[1]) / ImageHeight;
                                var objectName = eventData.Labels[(int)det[5]];
                                return new BoundingBoxInfo
                                {
                                    X = x,
                                    Y = y,
                                    Width = width,
                                    Height = height,
                                    ObjectName = objectName
                                };
                            })
                            .ToList()
                    };

                this.EventReceived?.Invoke(this, args);
            }
            catch
            {
                // Ignore
            }

            return Task.CompletedTask;
        }
    }
}
