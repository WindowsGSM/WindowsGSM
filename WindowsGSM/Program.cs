using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WindowsGSM.Services;
using MudBlazor.Services;
using WindowsGSM.GameServers;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using WindowsGSM.Utilities;
using System.Diagnostics;
using MudBlazor;
using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
    Args = args
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<GameServerService>();
builder.Services.AddSingleton<IHostedService>(p => p.GetRequiredService<GameServerService>());
builder.Services.AddSingleton<SystemMetricsService>();
//builder.Services.AddSingleton<IHostedService>(p => p.GetService<SystemMetricsService>()!);
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Host.UseWindowsService();
builder.Host.UseSerilog(new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
