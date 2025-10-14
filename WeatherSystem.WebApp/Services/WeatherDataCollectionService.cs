using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Concurrent;
using WeatherSystem.Grpc.Server.Protos;
using WeatherSystem.WebApp.Models;

namespace WeatherSystem.WebApp.Services
{
    public class WeatherDataCollectionService : IWeatherDataCollectionService, IDisposable
    {
        private readonly ILogger<WeatherDataCollectionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, WeatherNodeData> _weatherData = new();
        private readonly List<CancellationTokenSource> _cancellationTokens = new();
        private readonly List<Task> _subscriptionTasks = new();
        // Dictionary to keep track of the last successful heartbeat time for each node
        private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeats = new();
        // Heartbeat interval (10 seconds)
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(10);
        // Heartbeat timeout (if no response within 5 seconds, consider the node down)
        private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(5);
        // Connection retry delay when a node is unavailable
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

        public event Action? OnDataUpdated;

        // Node configuration - these should match the AppHost configuration
        private readonly Dictionary<string, string> _nodeEndpoints = new()
        {
            { "WeatherNode-0", "http://localhost:5001" },
            { "WeatherNode-1", "http://localhost:5002" },
            { "WeatherNode-2", "http://localhost:5003" },
            { "WeatherNode-3", "http://localhost:5004" }
        };

        public WeatherDataCollectionService(
            ILogger<WeatherDataCollectionService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Initialize weather data for all nodes
            foreach (var nodeEndpoint in _nodeEndpoints)
            {
                _weatherData[nodeEndpoint.Key] = new WeatherNodeData
                {
                    NodeName = nodeEndpoint.Key,
                    ConnectionStatus = "Not Connected",
                    IsConnected = false
                };
            }
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting weather data collection from all nodes...");

            foreach (var nodeEndpoint in _nodeEndpoints)
            {
                var cts = new CancellationTokenSource();
                _cancellationTokens.Add(cts);

                var task = ConnectToNodeAsync(nodeEndpoint.Key, nodeEndpoint.Value, cts.Token);
                _subscriptionTasks.Add(task);

                // Start a heartbeat task for each node
                var heartbeatTask = NodeHeartbeatAsync(nodeEndpoint.Key, nodeEndpoint.Value, cts.Token);
                _subscriptionTasks.Add(heartbeatTask);
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping weather data collection...");

            // Cancel all subscriptions
            foreach (var cts in _cancellationTokens)
            {
                cts.Cancel();
            }

            // Wait for all tasks to complete
            try
            {
                await Task.WhenAll(_subscriptionTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }

            _cancellationTokens.Clear();
            _subscriptionTasks.Clear();
        }

        private async Task ConnectToNodeAsync(string nodeName, string endpoint, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if the node is available first before trying to connect
                    if (!await IsNodeAvailableAsync(endpoint, cancellationToken))
                    {
                        UpdateConnectionStatus(nodeName, "Node Unavailable", false);
                        _logger.LogWarning("Node {NodeName} is unavailable. Retrying in {RetryDelay} seconds...", 
                            nodeName, _retryDelay.TotalSeconds);
                        await Task.Delay(_retryDelay, cancellationToken);
                        continue;
                    }

                    _logger.LogInformation("Connecting to weather node {NodeName} at {Endpoint}", nodeName, endpoint);

                    using var channel = GrpcChannel.ForAddress(endpoint);
                    var client = new WeatherBroadcast.WeatherBroadcastClient(channel);

                    var subscriptionRequest = new SubscriptionRequest
                    {
                        ClientId = $"WebApp-{nodeName}-{Guid.NewGuid().ToString()[..8]}"
                    };

                    UpdateConnectionStatus(nodeName, "Connecting...", false);

                    using var call = client.SubscribeToWeatherUpdates(subscriptionRequest, cancellationToken: cancellationToken);

                    // Successfully connected, update status and record heartbeat
                    UpdateConnectionStatus(nodeName, "Connected", true);
                    UpdateLastHeartbeat(nodeName);
                    _logger.LogInformation("Successfully connected to {NodeName}", nodeName);

                    await foreach (var measurement in call.ResponseStream.ReadAllAsync(cancellationToken))
                    {
                        // Update heartbeat on each message received
                        UpdateLastHeartbeat(nodeName);
                        UpdateWeatherData(nodeName, measurement);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    UpdateConnectionStatus(nodeName, "Node Unavailable", false);
                    _logger.LogWarning("Node {NodeName} is unavailable. Retrying in {RetryDelay} seconds...", 
                        nodeName, _retryDelay.TotalSeconds);
                    await Task.Delay(_retryDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    UpdateConnectionStatus(nodeName, "Disconnected", false);
                    _logger.LogInformation("Connection to {NodeName} was cancelled", nodeName);
                    break;
                }
                catch (Exception ex)
                {
                    UpdateConnectionStatus(nodeName, $"Error: {ex.Message}", false);
                    _logger.LogError(ex, "Error connecting to {NodeName}. Retrying in 10 seconds...", nodeName);
                    await Task.Delay(10000, cancellationToken);
                }
            }
        }

        // Check if a node is available by trying to establish a connection
        private async Task<bool> IsNodeAvailableAsync(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(_heartbeatTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                
                using var channel = GrpcChannel.ForAddress(endpoint);
                var client = new WeatherBroadcast.WeatherBroadcastClient(channel);
                
                // We're using a ping request just to check connectivity
                // We just need to see if the channel can connect, we don't need to wait for data
                var connectTask = Task.Run(async () => {
                    try {
                        // Try to establish a connection but immediately dispose it
                        using var call = client.SubscribeToWeatherUpdates(
                            new SubscriptionRequest { ClientId = $"ping-{Guid.NewGuid():N}" }, 
                            cancellationToken: linkedCts.Token);
                        // Try to read at least one message to verify server is responding
                        var enumerator = call.ResponseStream.ReadAllAsync(linkedCts.Token).GetAsyncEnumerator();
                        await enumerator.MoveNextAsync();
                        return true;
                    } 
                    catch {
                        return false;
                    }
                });
                
                // Add a timeout to the connection attempt
                if (await Task.WhenAny(connectTask, Task.Delay(_heartbeatTimeout, cancellationToken)) == connectTask)
                {
                    return await connectTask;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Periodic heartbeat check for each node
        private async Task NodeHeartbeatAsync(string nodeName, string endpoint, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if the node's last heartbeat is too old
                    if (_lastHeartbeats.TryGetValue(nodeName, out var lastHeartbeat))
                    {
                        var timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeat;
                        
                        // If the node was previously connected but we haven't heard from it in a while
                        if (timeSinceLastHeartbeat > _heartbeatInterval * 2)
                        {
                            var currentStatus = _weatherData.TryGetValue(nodeName, out var data) ? data.ConnectionStatus : "Unknown";
                            var isCurrentlyConnected = _weatherData.TryGetValue(nodeName, out data) && data.IsConnected;
                            
                            // Only update if we currently think it's connected
                            if (isCurrentlyConnected || currentStatus == "Connected")
                            {
                                _logger.LogWarning("Node {NodeName} heartbeat timeout after {Seconds}s. Last heartbeat: {LastHeartbeat}",
                                    nodeName, timeSinceLastHeartbeat.TotalSeconds, lastHeartbeat);
                                
                                UpdateConnectionStatus(nodeName, "Node Unavailable", false);
                            }
                        }
                    }

                    // Check connectivity to the node actively
                    var isAvailable = await IsNodeAvailableAsync(endpoint, cancellationToken);
                    
                    if (!isAvailable)
                    {
                        // Only update if we currently think it's connected
                        if (_weatherData.TryGetValue(nodeName, out var data) && data.IsConnected)
                        {
                            _logger.LogWarning("Heartbeat check failed for {NodeName}. Marking as unavailable.", nodeName);
                            UpdateConnectionStatus(nodeName, "Node Unavailable", false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in heartbeat check for {NodeName}", nodeName);
                }

                // Wait before next heartbeat check
                await Task.Delay(_heartbeatInterval, cancellationToken);
            }
        }

        private void UpdateWeatherData(string nodeName, WeatherMeasurement measurement)
        {
            var weatherData = new WeatherNodeData
            {
                NodeName = nodeName,
                SimulatorId = "Unknown", // We'll get this from the measurement context if available
                Timestamp = measurement.Timestamp,
                DayPhase = measurement.DayPhase,
                Temperature = measurement.Temperature,
                Humidity = measurement.Humidity,
                Pressure = measurement.Pressure,
                IsRaining = measurement.IsRaining,
                LastUpdated = DateTime.UtcNow,
                IsConnected = true,
                ConnectionStatus = "Connected"
            };

            _weatherData[nodeName] = weatherData;
            
            _logger.LogDebug("Updated weather data for {NodeName}: Temp={Temperature}°C, Humidity={Humidity}%", 
                nodeName, measurement.Temperature, measurement.Humidity);

            // Update last heartbeat time
            UpdateLastHeartbeat(nodeName);

            // Notify subscribers that data was updated
            OnDataUpdated?.Invoke();
        }

        private void UpdateConnectionStatus(string nodeName, string status, bool isConnected)
        {
            if (_weatherData.TryGetValue(nodeName, out var existingData))
            {
                existingData.ConnectionStatus = status;
                existingData.IsConnected = isConnected;
                existingData.LastUpdated = DateTime.UtcNow;
                
                // Notify subscribers that connection status changed
                OnDataUpdated?.Invoke();
            }
        }

        private void UpdateLastHeartbeat(string nodeName)
        {
            _lastHeartbeats[nodeName] = DateTime.UtcNow;
        }

        public IEnumerable<WeatherNodeData> GetAllWeatherData()
        {
            return _weatherData.Values.ToList();
        }

        public WeatherNodeData? GetWeatherDataByNode(string nodeName)
        {
            return _weatherData.TryGetValue(nodeName, out var data) ? data : null;
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            
            foreach (var cts in _cancellationTokens)
            {
                cts.Dispose();
            }
            _cancellationTokens.Clear();
        }
    }
}