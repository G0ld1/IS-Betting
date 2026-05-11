using BetStrike.Middleware.Contracts;
using MassTransit;
using BetStrike.Middleware.Worker;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<AnalyticsRepository>();
builder.Services.AddSingleton<PlatformEventProcessor>();
builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddConsumer<PlatformEventConsumer>();
    busConfigurator.AddConsumer<AlertRaisedEventConsumer>();
    busConfigurator.AddConsumer<DashboardRefreshConsumer>();

    busConfigurator.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMq:Password"] ?? "guest";
        var virtualHost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";

        cfg.Host(host, virtualHost, hostConfigurator =>
        {
            hostConfigurator.Username(username);
            hostConfigurator.Password(password);
        });

        cfg.ReceiveEndpoint(MiddlewareTopology.StreamQueue, endpoint =>
        {
            endpoint.PrefetchCount = 16;
            endpoint.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(2)));
            endpoint.UseDelayedRedelivery(retry => retry.Intervals(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
            endpoint.UseInMemoryOutbox();
            endpoint.SetQueueArgument("x-dead-letter-exchange", "");
            endpoint.SetQueueArgument("x-dead-letter-routing-key", MiddlewareTopology.StreamDeadLetterQueue);
            endpoint.ConfigureConsumer<PlatformEventConsumer>(context);
        });

        cfg.ReceiveEndpoint(MiddlewareTopology.StreamDeadLetterQueue, endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
        });

        cfg.ReceiveEndpoint(MiddlewareTopology.HighPriorityAnalyticsQueue, endpoint =>
        {
            endpoint.PrefetchCount = 4;
            endpoint.UseMessageRetry(retry => retry.Interval(5, TimeSpan.FromSeconds(3)));
            endpoint.UseDelayedRedelivery(retry => retry.Intervals(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)));
            endpoint.UseInMemoryOutbox();
            endpoint.SetQueueArgument("x-dead-letter-exchange", "");
            endpoint.SetQueueArgument("x-dead-letter-routing-key", MiddlewareTopology.HighPriorityAnalyticsDeadLetterQueue);
            endpoint.ConfigureConsumer<DashboardRefreshConsumer>(context);
        });

        cfg.ReceiveEndpoint(MiddlewareTopology.HighPriorityAnalyticsDeadLetterQueue, endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
        });

        cfg.ReceiveEndpoint(MiddlewareTopology.LowPriorityAnalyticsQueue, endpoint =>
        {
            endpoint.PrefetchCount = 2;
            endpoint.UseMessageRetry(retry => retry.Interval(5, TimeSpan.FromSeconds(5)));
            endpoint.UseDelayedRedelivery(retry => retry.Intervals(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)));
            endpoint.UseInMemoryOutbox();
            endpoint.SetQueueArgument("x-dead-letter-exchange", "");
            endpoint.SetQueueArgument("x-dead-letter-routing-key", MiddlewareTopology.LowPriorityAnalyticsDeadLetterQueue);
            endpoint.ConfigureConsumer<DashboardRefreshConsumer>(context);
        });

        cfg.ReceiveEndpoint(MiddlewareTopology.LowPriorityAnalyticsDeadLetterQueue, endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
        });
    });
});

builder.Services.AddHostedService<HistoricalReplayService>();

await builder.Build().RunAsync();