using Neon.Core.Data.Twitch;
using Neon.Core.Extensions;
using Neon.Core.Services.Kafka;
using Neon.Obs.BrowserSource.WebApp.Consumers;
using Neon.Obs.BrowserSource.WebApp.Hubs;
using Neon.Obs.BrowserSource.WebApp.Models;
using Neon.Obs.BrowserSource.WebApp.Services;
using Neon.Obs.BrowserSource.WebApp.Services.Events;
using Neon.Obs.BrowserSource.WebApp.Services.StreamElements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.ConfigureSerilog(builder.Configuration);
builder.Services.ConfigureNeonDbContext(builder.Configuration);

builder.Services.Configure<BaseKafkaConfig>(builder.Configuration.GetSection("BaseKafkaConfig"));
builder.Services.AddSingleton<IKafkaService, KafkaService>();

builder.Services.AddScoped<ITwitchDbService, TwitchDbService>();
builder.Services.AddScoped<ITwitchChatOverlayService, TwitchChatOverlayService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IStreamElementsEventService, StreamElementsEventService>();

builder.Services.AddHostedService<TwitchMessageConsumer>();
builder.Services.AddHostedService<TwitchEventConsumer>();
builder.Services.AddHostedService<StreamElementsEventConsumer>();

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.UseEndpoints(e =>
{
    e.MapRazorPages();
    e.MapHub<ChatHub>("/twitchchat");
});

app.Run();