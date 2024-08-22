using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOHO.Core.Entities;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
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
    private readonly ILogger<EventListener> _logger;
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

    public EventListener(ILogger<EventListener> logger, BOHOConfiguration configuration)
    {
        _logger = logger;
        _mqttTopic = configuration.MqttTopic;
        _mqttHost = configuration.MqttHost;
        _mqttPort = configuration.MqttPort;
        _imageWidth = configuration.AnalyticImageWidth;
        _imageHeight = configuration.AnalyticImageHeight;

        _mqttClient = new MqttFactory().CreateManagedMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
    }

    public void Dispose()
    {
        _mqttClient.ApplicationMessageReceivedAsync -= MqttClient_ApplicationMessageReceivedAsync;
        _mqttClient.Dispose();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("The connection has been initialized");
        }

        IEnumerable<MqttTopicFilter> topicFilters = new string[]
        {
            "/test/milestone",
            _mqttTopic
        }.Select(topic => new MqttTopicFilterBuilder().WithTopic(topic).Build());
        await _mqttClient.SubscribeAsync(topicFilters.ToList());

        // Setup and start a managed MQTT client.
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId("MilestonePluginClient-" + Guid.NewGuid().ToString())
            .WithTcpServer(_mqttHost, _mqttPort)
            .Build();
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(clientOptions)
            .Build();
        await _mqttClient.StartAsync(options);

        _isInitialized = true;
    }

    private Task MqttClient_ApplicationMessageReceivedAsync(
        MqttApplicationMessageReceivedEventArgs arg
    )
    {
        var payloadString = Encoding.UTF8.GetString([.. arg.ApplicationMessage.PayloadSegment]);

        _logger.LogDebug("Received an event: {Event}", payloadString);

        try
        {
            JObject jsonData = JObject.Parse(payloadString);
            BOHOEventArgs args =
                new()
                {
                    DeviceId = jsonData.ContainsKey("bounding_box")
                        ? 84
                        : jsonData["camera_id"].ToObject<int>(),
                    DeviceName = jsonData["camera_name"].ToString(),
                    BoundingBoxes = jsonData.ContainsKey("bounding_box")
                        ?
                        [
                            new BoundingBox
                            {
                                TrackingNumber = jsonData["tracking_number"].ToObject<int>(),
                                X =
                                    jsonData["bounding_box"]["topleftx"].ToObject<int>()
                                    / _imageWidth,
                                Y =
                                    jsonData["bounding_box"]["toplefty"].ToObject<int>()
                                    / _imageHeight,
                                Width =
                                    (
                                        jsonData["bounding_box"]["bottomrightx"].ToObject<int>()
                                        - jsonData["bounding_box"]["topleftx"].ToObject<int>()
                                    ) / _imageWidth,
                                Height =
                                    (
                                        jsonData["bounding_box"]["bottomrighty"].ToObject<int>()
                                        - jsonData["bounding_box"]["toplefty"].ToObject<int>()
                                    ) / _imageHeight,
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

            EventReceived?.Invoke(this, args);
        }
        catch
        {
            // Ignore
        }

        return Task.CompletedTask;
    }
}
