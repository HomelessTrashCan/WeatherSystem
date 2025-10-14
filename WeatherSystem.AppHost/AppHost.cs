using System.Xml.Linq;

var builder = DistributedApplication.CreateBuilder(args);

// Define nodes with their HTTP endpoints and unique node names
var node0 = builder.AddProject<Projects.WeatherSystem_Node>("weathersystem-node0")
    .WithHttpEndpoint(5001, name: "grpc")
    .WithEnvironment("NodeSettings__NodeName", "WeatherNode-0");

var node1 = builder.AddProject<Projects.WeatherSystem_Node>("weathersystem-node1")
    .WithHttpEndpoint(5002, name: "grpc")
    .WithEnvironment("NodeSettings__NodeName", "WeatherNode-1");

var node2 = builder.AddProject<Projects.WeatherSystem_Node>("weathersystem-node2")
    .WithHttpEndpoint(5003, name: "grpc")
    .WithEnvironment("NodeSettings__NodeName", "WeatherNode-2");

var node3 = builder.AddProject<Projects.WeatherSystem_Node>("weathersystem-node3")
    .WithHttpEndpoint(5004, name: "grpc")
    .WithEnvironment("NodeSettings__NodeName", "WeatherNode-3");

// Configure simulators to reference their corresponding nodes with unique simulator IDs
builder.AddProject<Projects.WeatherSystem_Simulator>("weathersystem-simulator0")
    .WithReference(node0)
    .WithEnvironment("WeatherPublisher__GrpcServiceUrl", node0.GetEndpoint("grpc"))
    .WithEnvironment("WeatherPublisher__SimulatorId", "Simulator-0");

builder.AddProject<Projects.WeatherSystem_Simulator>("weathersystem-simulator1")
    .WithReference(node1)
    .WithEnvironment("WeatherPublisher__GrpcServiceUrl", node1.GetEndpoint("grpc"))
    .WithEnvironment("WeatherPublisher__SimulatorId", "Simulator-1");

builder.AddProject<Projects.WeatherSystem_Simulator>("weathersystem-simulator2")
    .WithReference(node2)
    .WithEnvironment("WeatherPublisher__GrpcServiceUrl", node2.GetEndpoint("grpc"))
    .WithEnvironment("WeatherPublisher__SimulatorId", "Simulator-2");

builder.AddProject<Projects.WeatherSystem_Simulator>("weathersystem-simulator3")
    .WithReference(node3)
    .WithEnvironment("WeatherPublisher__GrpcServiceUrl", node3.GetEndpoint("grpc"))
    .WithEnvironment("WeatherPublisher__SimulatorId", "Simulator-3");

// Add the WebApp that will collect data from all nodes
builder.AddProject<Projects.WeatherSystem_WebApp>("weathersystem-webapp")
    .WithReference(node0)
    .WithReference(node1)
    .WithReference(node2)
    .WithReference(node3)
    .WithHttpEndpoint(5000, name: "http");

builder.Build().Run();
