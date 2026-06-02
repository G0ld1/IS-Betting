using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Infrastructure;
using MassTransit;
using Microsoft.Data.SqlClient;

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
builder.Services.AddSingleton<IKafkaEventPublisher, KafkaEventPublisher>();
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

app.MapGet("/health/live", () => Results.Ok(new
{
	service = "BetStrike.Betting.Api",
	status = "ok",
	timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health/ready", async (IConfiguration configuration, CancellationToken ct) =>
{
	var connectionString = configuration.GetConnectionString("ApostasDb");
	if (string.IsNullOrWhiteSpace(connectionString))
	{
		return Results.Problem("Connection string ApostasDb não configurada.", statusCode: StatusCodes.Status503ServiceUnavailable);
	}

	try
	{
		await using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync(ct);
		return Results.Ok(new
		{
			service = "BetStrike.Betting.Api",
			status = "ready",
			database = "ok",
			broker = "RabbitMQ",
			timestampUtc = DateTime.UtcNow
		});
	}
	catch (Exception ex)
	{
		return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
	}
});

app.MapControllers();
app.Run();
