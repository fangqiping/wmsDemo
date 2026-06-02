using System.Globalization;
using Backend.Demo.DependencyInjection;
using Microsoft.AspNetCore.Localization;
using FlowEngine.Server.Authorization.Permissions;
using FlowEngine.Server.Notifications;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BackendDemo")
    ?? "Data Source=backend-demo.db";
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? [
        "http://127.0.0.1:5173",
        "http://localhost:5173",
    ];

builder.Services.AddBackendDemoApplication(connectionString);
builder.Services.AddNotificationRealtime();
builder.Services.AddAuthorization(options => {
    options.AddPolicy(PermissionsConstants.PolicyName, policy => {
        policy.RequireAssertion(_ => true);
    });
});
builder.Services.AddCors(options => {
    options.AddPolicy("FlowView", policy => {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
var supportedCultures = new[] {
    new CultureInfo("en-US"),
    new CultureInfo("zh-Hans-CN"),
};
builder.Services.Configure<RequestLocalizationOptions>(options => {
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRequestLocalization();

await app.Services.InitializeBackendDemoAsync();

app.UseCors("FlowView");
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapNotificationHub();

app.Run();

public partial class Program { }
