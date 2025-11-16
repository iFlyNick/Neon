using Neon.Core.Models.Twitch.EventSub;
using Neon.Obs.BrowserSource.WebApp.Models;

namespace Neon.Obs.BrowserSource.WebApp.Services.Events;

public interface IEventService
{
    TwitchEventMessage? ProcessMessage(Message? message);
}