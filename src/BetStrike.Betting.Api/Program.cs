using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Infrastructure;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

DapperTypeHandlers.Configure();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
	options.AddPolicy("LocalFrontend", policy =>
		policy.AllowAnyOrigin()
			  .AllowAnyHeader()
			  .AllowAnyMethod());
});

builder.Services.AddScoped<IBettingRepository, BettingRepository>();
builder.Services.AddScoped<IBettingService, BettingService>();
builder.Services.AddMassTransit(busConfigurator =>
{
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
	});
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("LocalFrontend");

app.MapControllers();
app.Run();
