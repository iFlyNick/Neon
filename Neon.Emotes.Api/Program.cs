using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Emotes.Api.Services.Abstractions;
using Neon.Emotes.Api.Services.BetterTtv;
using Neon.Emotes.Api.Services.Emote;
using Neon.Emotes.Api.Services.FrankerFaceZ;
using Neon.Emotes.Api.Services.SevenTv;
using Neon.Emotes.Api.Services.Twitch;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureSerilog(builder.Configuration);
builder.Services.ConfigureNeonDbContext(builder.Configuration);
builder.Services.ConfigureTwitchServices(builder.Configuration);

builder.Services.Configure<TwitchSettings>(builder.Configuration.GetSection("TwitchSettings"));
builder.Services.Configure<NeonSettings>(builder.Configuration.GetSection("NeonSettings"));

builder.Services.AddHttpClient<IHttpService, HttpService>();

builder.Services.AddScoped<IEmoteService, EmoteService>();
builder.Services.AddScoped<IIntegratedEmoteService, TwitchService>();
builder.Services.AddScoped<IIntegratedEmoteService, BetterTtvService>();
builder.Services.AddScoped<IIntegratedEmoteService, FrankerFaceZService>();
builder.Services.AddScoped<IIntegratedEmoteService, SevenTvService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
