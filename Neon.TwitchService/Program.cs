using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Extensions;
using Neon.Core.Models;
using Neon.Core.Models.Kafka;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Twitch.Authentication;
using Neon.Persistence.EntityModels.Twitch;
using Neon.TwitchService.Consumers;
using Neon.TwitchService.Services.WebSockets;
using Newtonsoft.Json;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.ConfigureSerilog(hostContext.Configuration);
        services.ConfigureNeonDbContext(hostContext.Configuration);
        services.ConfigureTwitchServices(hostContext.Configuration);

        services.Configure<TwitchSettings>(hostContext.Configuration.GetSection("TwitchSettings"));
        services.Configure<NeonSettings>(hostContext.Configuration.GetSection("NeonSettings"));

        //use named clients instead to avoid swapping auth headers around
        services.AddHttpClient<IHttpService, HttpService>();

        services.AddTransient<IKafkaService, KafkaService>();

        services.AddScoped<IBotTokenService, BotTokenService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddTransient<IWebSocketService, WebSocketService>();

        services.AddHostedService<ChannelConsumer>();
    })
    .Build();

using var scope = host.Services.CreateScope();

var kafkaServiceChats = scope.ServiceProvider.GetRequiredService<IKafkaService>();
var kafkaServiceEvents = scope.ServiceProvider.GetRequiredService<IKafkaService>();

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dbService = scope.ServiceProvider.GetRequiredService<ITwitchDbService>();
var oauth = scope.ServiceProvider.GetRequiredService<IOAuthService>();
var appAuth = scope.ServiceProvider.GetRequiredService<IBotTokenService>();

var twitchSettings = scope.ServiceProvider.GetRequiredService<IOptions<TwitchSettings>>().Value;
var botSettings = scope.ServiceProvider.GetRequiredService<IOptions<NeonSettings>>().Value;

var appAccount = await dbService.GetBotAccountAsync(botSettings.BotName);
var appAuthResp = await appAuth.GetBotAccountAuthAsync();

ArgumentNullException.ThrowIfNull(appAccount, nameof(appAccount));
ArgumentNullException.ThrowIfNull(appAuthResp, nameof(appAuthResp));

var appTwitchAccount = new NeonTwitchBotSettings
{
    Username = appAccount.BotName,
    ClientId = appAccount.ClientId,
    ClientSecret = appAccount.ClientSecret,
    AccessToken = appAccount.AccessToken,
    RedirectUri = appAccount.RedirectUri,
    BroadcasterId = appAccount.TwitchBroadcasterId
};

var activeAccounts = await dbService.GetSubscribedTwitchAccountsAsync();

var chatBotAccount = await dbService.GetNeonBotTwitchAccountAsync();
ArgumentNullException.ThrowIfNull(chatBotAccount, nameof(chatBotAccount));

string? chatBotAccessToken = null;
var chatOAuthValidation = await oauth.ValidateOAuthToken(chatBotAccount.AccessToken);

if (chatOAuthValidation is not null)
{
    logger.LogDebug("Chat bot access token is valid for account: {account}", chatBotAccount.DisplayName);
    chatBotAccessToken = chatBotAccount.AccessToken;
}
else
{
    if (string.IsNullOrEmpty(chatBotAccessToken))
    {
        logger.LogInformation("Refreshing access token for account: {account}", chatBotAccount.DisplayName);

        var chatOAuthResp = await oauth.GetUserAuthTokenFromRefresh(appTwitchAccount.ClientId, appTwitchAccount.ClientSecret, chatBotAccount.RefreshToken);

        if (chatOAuthResp is null || string.IsNullOrEmpty(chatOAuthResp.AccessToken))
        {
            logger.LogError("Failed to refresh access token for account: {account}", chatBotAccount.DisplayName);
        }

        await dbService.UpdateTwitchAccountAuthAsync(chatBotAccount.BroadcasterId, chatOAuthResp!.AccessToken);
        chatBotAccessToken = chatOAuthResp.AccessToken;
    }
}

//add random accounts for testing
var fakeAccountList = new List<TwitchAccount>()
{
    new() { BroadcasterId = "109690289" }, //steve
    //new() { BroadcasterId = "92038375" }, //caedrel
    //new() { BroadcasterId = "151368796" }, //piratesoftware
    //new() { BroadcasterId = "15564828" }, //nickmercs
    //new() { BroadcasterId = "57781936" }, //rocketleague
    //new() { BroadcasterId = "124422593" }, //lec
    //new() { BroadcasterId = "124420521" }, //ltanorth
    //new() { BroadcasterId = "40017619" }, //doublelift
};

//was randomly top 20 channels for fun
//var fakeAccountList = new List<TwitchAccount>()
//{
//    new() { BroadcasterId = "851088609" },
//    new() { BroadcasterId = "28575692" },
//    new() { BroadcasterId = "86277097" },
//    new() { BroadcasterId = "552120296" },
//    new() { BroadcasterId = "77964394" },
//    new() { BroadcasterId = "622498423" },
//    new() { BroadcasterId = "39276140" },
//    new() { BroadcasterId = "50985620" },
//    new() { BroadcasterId = "36481935" },
//    new() { BroadcasterId = "107117952" },
//    new() { BroadcasterId = "57781936" },
//    new() { BroadcasterId = "238813810" },
//    new() { BroadcasterId = "42316376" },
//    new() { BroadcasterId = "411377640" },
//    new() { BroadcasterId = "23161357" },
//    new() { BroadcasterId = "89132304" },
//    new() { BroadcasterId = "45044816" },
//    new() { BroadcasterId = "43683025" },
//    new() { BroadcasterId = "137512364" },
//    new() { BroadcasterId = "233741947" }
//};

if (activeAccounts is not null && activeAccounts.Count > 0)
{
    //setup app account for websocket chat events
    var chatWs = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
    chatWs.SetNeonTwitchBotSettings(appTwitchAccount);

    var kafkaProducerConfigChat = new KafkaProducerConfig
    {
        Topic = "twitch-chat-events",
        TargetPartition = "0",
        BootstrapServers = "localhost:9092"
    };

    await chatWs.ConnectAsync(async message =>
    {
        //logger.LogInformation("Received message: {message}", message);
        await kafkaServiceChats.ProduceAsync(kafkaProducerConfigChat, JsonConvert.SerializeObject(message));
    });

    //delay for websocket to send first message
    await Task.Delay(2000);

    var fullFakeAccountList = new List<TwitchAccount>();
    fullFakeAccountList.AddRange(fakeAccountList);
    fullFakeAccountList.AddRange(activeAccounts);

    foreach (var account in fullFakeAccountList)
        await chatWs.SubscribeChannelChatAsync(account.BroadcasterId, chatBotAccessToken, null);

    foreach (var account in activeAccounts)
    {
        var ws = scope.ServiceProvider.GetRequiredService<IWebSocketService>();

        ws.SetNeonTwitchBotSettings(appTwitchAccount);

        var kafkaProducerConfigEvents = new KafkaProducerConfig
        {
            Topic = "twitch-channel-events",
            TargetPartition = "0",
            BootstrapServers = "localhost:9092"
        };

        await ws.ConnectAsync(async message =>
        {
            await kafkaServiceEvents.ProduceAsync(kafkaProducerConfigEvents, JsonConvert.SerializeObject(message));
        });

        string? accessToken = null;
        var oAuthValidation = await oauth.ValidateOAuthToken(account.AccessToken);

        if (oAuthValidation is not null)
        {
            logger.LogDebug("Access token is valid for account: {account}", account.DisplayName);
            accessToken = account.AccessToken;
            await ws.SubscribeChannelAsync(account.BroadcasterId, accessToken, null);
            continue;
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogInformation("Refreshing access token for account: {account}", account.DisplayName);
            var oAuthResp = await oauth.GetUserAuthTokenFromRefresh(appTwitchAccount.ClientId, appTwitchAccount.ClientSecret, account.RefreshToken);

            if (oAuthResp is null || string.IsNullOrEmpty(oAuthResp.AccessToken))
            {
                logger.LogError("Failed to refresh access token for account: {account}", account.DisplayName);
                continue;
            }

            await dbService.UpdateTwitchAccountAuthAsync(account.BroadcasterId, oAuthResp.AccessToken);

            accessToken = oAuthResp.AccessToken;
        }

        await ws.SubscribeChannelAsync(account.BroadcasterId, accessToken, null);
    }
}

await host.RunAsync();
