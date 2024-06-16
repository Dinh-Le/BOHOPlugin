using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BOHO.Core.Entities;
using BOHO.Core.Interfaces;
using Newtonsoft.Json;

namespace BOHO.Infrastructure.Repositories
{
    public class BOHORepository : IBOHORepository
    {
        public List<Core.Entities.Node> Nodes { get; set; }

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BOHOConfiguration _configuration;
        private string _accessToken;

        #region Models
        private class UserLoginRequest
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }
        }

        private class Response<T>
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("data")]
            public T Data { get; set; }
        }

        private class Node
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }

        private class Device
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        private class Integration
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("milestone_id")]
            public int MilestoneId { get; set; }

            [JsonProperty("guid")]
            public string Guid { get; set; }
        }
        #endregion


        public BOHORepository(
            IHttpClientFactory httppClientFactory,
            BOHOConfiguration configuration
        )
        {
            this._httpClientFactory = httppClientFactory;
            this._configuration = configuration;
            this.Nodes = new List<Core.Entities.Node>();
        }

        public async Task Synchronize()
        {
            this.Nodes.Clear();

            using (var httpClient = this._httpClientFactory.CreateClient())
            {
                var getNodesUrl =
                    $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node";
                var getNodesResponse = await this.GetJson<Response<List<Node>>>(
                    httpClient,
                    getNodesUrl
                );
                var nodes = getNodesResponse
                    .Data.Where(node => node.Type.Equals("TensorTRT"))
                    .ToList();

                foreach (var node in nodes)
                {
                    var deviceList = new List<Core.Entities.Device>();
                    var getDevicesUrl =
                        $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node/{node.Id}/device";
                    var getDevicesResponse = await this.GetJson<Response<List<Device>>>(
                        httpClient,
                        getDevicesUrl
                    );

                    foreach (var device in getDevicesResponse.Data)
                    {
                        var getIntegrationsUrl =
                            $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node/{node.Id}/device/{device.Id}/intergrate";
                        var getIntegrationsResponse = await this.GetJson<
                            Response<List<Integration>>
                        >(httpClient, getIntegrationsUrl);
                        var integration = getIntegrationsResponse.Data.FirstOrDefault(x =>
                            x.MilestoneId == this._configuration.MilestoneId
                        );
                        if (integration != default(Integration))
                        {
                            deviceList.Add(
                                new Core.Entities.Device
                                {
                                    ID = device.Id,
                                    Name = device.Name,
                                    Guid = integration.Guid,
                                    NodeID = node.Id
                                }
                            );
                        }
                    }

                    if (!deviceList.Any())
                    {
                        continue;
                    }

                    this.Nodes.Add(
                        new Core.Entities.Node
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Devices = deviceList
                        }
                    );
                }
            }
        }

        public async Task Login()
        {
            using (var httpClient = this._httpClientFactory.CreateClient())
            {
                var payloadString = JsonConvert.SerializeObject(
                    new UserLoginRequest
                    {
                        Name = _configuration.Username,
                        Password = _configuration.Password
                    }
                );
                var response = await httpClient.PostAsync(
                    $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/login",
                    new StringContent(payloadString)
                );
                response.EnsureSuccessStatusCode();

                var responseText = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<Response<string>>(responseText);

                this._accessToken = loginResponse.Data;
            }
        }

        public async Task StartService(Core.Entities.Device device)
        {
            using (var httpClient = this._httpClientFactory.CreateClient())
            {
                var url =
                    $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node/{device.NodeID}/device/{device.ID}/resume_device";
                await this.GetJson<Response<object>>(httpClient, url);
            }
        }

        public async Task StopService(Core.Entities.Device device)
        {
            using (var httpClient = this._httpClientFactory.CreateClient())
            {
                var url =
                    $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node/{device.NodeID}/device/{device.ID}/pause_device";
                await this.GetJson<Response<object>>(httpClient, url);
            }
        }

        public async Task<bool> GetServiceStatus(Core.Entities.Device device)
        {
            using (var httpClient = this._httpClientFactory.CreateClient())
            {
                var url =
                    $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node/{device.NodeID}/device/{device.ID}/service_status";
                var response = await this.GetJson<Response<bool>>(httpClient, url);
                return response.Data;
            }
        }

        public async Task<IEnumerable<Rule>> GetRules(Core.Entities.Device device)
        {
            using (var httpClient = this._httpClientFactory.CreateClient())
            {
                var url =
                    $"http://{_configuration.IP}:{_configuration.ApiPort}/api/rest/v1/node/{device.NodeID}/device/{device.ID}/rule";
                var response = await this.GetJson<Response<IEnumerable<Rule>>>(httpClient, url);
                return response.Data;
            }
        }

        public async Task<T> GetJson<T>(HttpClient httpClient, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(this._accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        this._accessToken
                    );
            }

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseText);
        }
    }
}
