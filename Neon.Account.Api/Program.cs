using Neon.Account.Api.Services.Twitch;
using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IHttpService, HttpService>();

builder.Services.ConfigureSerilog(builder.Configuration);
builder.Services.ConfigureNeonDbContext(builder.Configuration);
builder.Services.ConfigureTwitchServices(builder.Configuration);

builder.Services.Configure<TwitchSettings>(builder.Configuration.GetSection("TwitchSettings"));
builder.Services.Configure<NeonSettings>(builder.Configuration.GetSection("NeonSettings"));

builder.Services.AddScoped<ITwitchAuthResponseService, TwitchAuthResponseService>();
builder.Services.AddScoped<ITwitchAccountService, TwitchAccountService>();

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
