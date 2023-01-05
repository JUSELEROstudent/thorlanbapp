using System.Xml.Serialization;
using GotsThorlabs.NodesApi;
using Thorlabs.MotionControl.DeviceManagerCLI;
using GotsThorlabs.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
SimulationManager.Instance.InitializeSimulations();
var variableapinode = new NodeGenerics(app);
var loginloginnodes = new NodeLogin(app);
var Nodehomepages = new NodeHomepage(app);


app.MapHub<ChatHub>("/chatHub");
app.MapHub<StreamingHub>("/StreamingHub");


app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}