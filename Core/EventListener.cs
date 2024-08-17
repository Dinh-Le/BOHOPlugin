using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOHO.Core.Entities;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BOHO.Core;

public interface IEventListener
{
    public event EventHandler<BOHOEventArgs> EventReceived;
    public Task InitializeAsync();
}

public class EventListener : IEventListener, IDisposable
{
    private readonly string _mqttTopic;
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly double _imageWidth;
    private readonly double _imageHeight;
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

        [JsonProperty("event_time")]
        public DateTime EventTime { get; set; }
    }

    public EventListener(BOHOConfiguration configuration)
    {
        this._mqttTopic = configuration.MqttTopic;
        this._mqttHost = configuration.MqttHost;
        this._mqttPort = configuration.MqttPort;
        this._imageWidth = configuration.AnalyticImageWidth;
        this._imageHeight = configuration.AnalyticImageHeight;

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

    public async Task InitializeAsync()
    {
        if (this._isInitialized)
        {
            throw new InvalidOperationException("The connection has been initialized");
        }

        var topicFilter = new MqttTopicFilterBuilder().WithTopic(_mqttTopic).Build();
        await this._mqttClient.SubscribeAsync([topicFilter]);

        // Setup and start a managed MQTT client.
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId("MilestonePluginClient-" + Guid.NewGuid().ToString())
            .WithTcpServer(_mqttHost, _mqttPort)
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
            JObject jsonData = JObject.Parse(payloadString);
            BOHOEventArgs args =
                new()
                {
                    DeviceId = jsonData["camera_id"].ToObject<int>(),
                    DeviceName = jsonData["camera_name"].ToString(),
                    BoundingBoxes = jsonData.ContainsKey("bounding_box")
                        ?
                        [
                            new BoundingBox
                            {
                                TrackingNumber = jsonData["tracking_number"].ToObject<int>(),
                                X =
                                    jsonData["bounding_box"]["topleftx"].ToObject<int>()
                                    / this._imageWidth,
                                Y =
                                    jsonData["bounding_box"]["toplefty"].ToObject<int>()
                                    / this._imageHeight,
                                Width =
                                    (
                                        jsonData["bounding_box"]["bottomrightx"].ToObject<int>()
                                        - jsonData["bounding_box"]["topleftx"].ToObject<int>()
                                    ) / this._imageWidth,
                                Height =
                                    (
                                        jsonData["bounding_box"]["bottomrighty"].ToObject<int>()
                                        - jsonData["bounding_box"]["toplefty"].ToObject<int>()
                                    ) / this._imageHeight,
                                ObjectName = jsonData["object_type"].ToString(),
                                Timestamp = jsonData["event_time"].ToObject<DateTime>()
                            }
                        ]
                        : jsonData["det"]
                            .ToObject<double[][]>()
                            .Select(det => new BoundingBox
                            {
                                X = det[0] / _imageWidth,
                                Y = det[1] / _imageHeight,
                                Width = (det[2] - det[0]) / _imageWidth,
                                Height = (det[3] - det[1]) / _imageHeight,
                                ObjectName = jsonData["labels"][(int)det[5]].ToString()
                            })
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
