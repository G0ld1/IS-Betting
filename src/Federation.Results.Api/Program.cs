using Federation.Results.Api.Application;
using Federation.Results.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

DapperTypeHandlers.Configure();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameService, GameService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
