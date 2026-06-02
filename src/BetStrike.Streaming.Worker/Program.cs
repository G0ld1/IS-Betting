using BetStrike.Streaming.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "BETSTRIKE_");

builder.Services.AddSingleton<StreamingAnalyticsRepository>();
builder.Services.AddSingleton<StreamingAlertService>();
builder.Services.AddHostedService<KafkaConsumerService>();

var host = builder.Build();

host.Run();
