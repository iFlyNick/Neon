using Neon.Obs.BrowserSource.WebApp.Models.StreamElements;

namespace Neon.Obs.BrowserSource.WebApp.Services.StreamElements;

public interface IStreamElementsEventService
{
    Task<StreamElementsEventMessage?> ProcessMessage(Message? message, CancellationToken ct = default);
}