using System.Xml.Serialization;
using GotsThorlabs.NodesApi;
using Thorlabs.MotionControl.DeviceManagerCLI;
using GotsThorlabs.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using apitest.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(options =>
{
    // El sistema de autenticacion de esta forma definido solo fuciona para la minimal api. cuando se desea hacer para la forma de webApi controlladores es mejor hacerlo de otra forma
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["CustomCOnfig:JWT_ISSUER_TOKEN"],
        ValidAudience = builder.Configuration["CustomCOnfig:JWT_AUDIENCE_TOKEN"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["CustomCOnfig:JWT_SECRET_KEY"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});


builder.Services.AddAuthorization((options) =>
{
    options.AddPolicy("Jwtvalidator", (policy) =>
    {
        policy.Requirements.Add(new Httpcontextentry(true));
    });
});


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

app.MapGet("/security/getMessageSecure", () => "Hello World!").RequireAuthorization();
app.UseHttpsRedirection();
SimulationManager.Instance.InitializeSimulations();
var variableapinode = new NodeGenerics(app);
var loginloginnodes = new NodeLogin(app);
var Nodehomepages = new NodeHomepage(app);
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");
app.MapHub<StreamingHub>("/StreamingHub");


app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}