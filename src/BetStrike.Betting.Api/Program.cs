using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

DapperTypeHandlers.Configure();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IBettingRepository, BettingRepository>();
builder.Services.AddScoped<IBettingService, BettingService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
