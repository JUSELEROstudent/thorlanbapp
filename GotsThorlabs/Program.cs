using System.Xml.Serialization;
using GotsThorlabs.NodesApi;
using GotsThorlabs.Stitchingapi;
//using GotsThorlabs.Tour;
using Thorlabs.MotionControl.DeviceManagerCLI;
using GotsThorlabs.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using apitest.Controllers;
using Microsoft.Extensions.FileProviders;
using GotsThorlabs;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using Microsoft.EntityFrameworkCore.Sqlite;  

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddSignalR();
// Camera services: each concrete service is a singleton (device-level locking inside).
// CameraServiceFactory selects the right one at runtime by driverType string.
builder.Services.AddSingleton<GotsThorlabs.Services.VideoCaptureCameraService>();
builder.Services.AddSingleton<GotsThorlabs.Services.IdsPeakCameraService>();
builder.Services.AddSingleton<GotsThorlabs.Services.CameraServiceFactory>();
// Default ICameraService resolves to the factory's generic driver.
builder.Services.AddSingleton<GotsThorlabs.Interfaces.ICameraService>(sp =>
    sp.GetRequiredService<GotsThorlabs.Services.CameraServiceFactory>().GetService("generic"));
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
SimulationManager.Instance.InitializeSimulations();
builder.Services.AddAuthorization((options) =>
{
    options.AddPolicy("Jwtvalidator", (policy) =>
    {
        policy.Requirements.Add(new Httpcontextentry(true));
    });
    options.AddPolicy("administrator", policy =>
        policy.Requirements.Add(new Authorizationadmin(4)));
});

var dbDir = Path.Combine(builder.Environment.ContentRootPath, "database");
Directory.CreateDirectory(dbDir);
var dbPath = Path.Combine(dbDir, "app.sqlite");

builder.Services.AddDbContext<ThorlabsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ThorlabsDbContext>();
    db.Database.Migrate(); // creates/apply migrations -> creates tables in app.sqlite
}

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

app.MapGet("/security/getMessageSecure", () => "Hello World!").RequireAuthorization(); //.RequireAuthorization("administrator"); esta parte es para agregar politicas no es necesario por ahora
app.MapGet("/security/getMessage2", () => "Hello World!");
app.UseHttpsRedirection();
//SimulationManager.Instance.InitializeSimulations();

var variableapinode = new NodeGenerics(app);
var loginloginnodes = new NodeLogin(app);
var Nodehomepages = new NodeHomepage(app);
var NodeMapping = new NodeMapping(app);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "StaticFiles")),
    RequestPath = "/SouerceStaticFiles"
});
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");
app.MapHub<StreamingHub>("/StreamingHub");
app.MapHub<UpdateStatus>("/UpdateStatus");

app.MapStitchingEndpoints();
app.MapTourEndpoints();


app.Run();
