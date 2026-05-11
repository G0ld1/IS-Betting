using Federation.Results.Api.Application;
using Federation.Results.Api.Infrastructure;
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

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameService, GameService>();
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
