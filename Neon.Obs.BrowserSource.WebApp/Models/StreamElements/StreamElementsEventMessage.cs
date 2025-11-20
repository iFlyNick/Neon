namespace Neon.Obs.BrowserSource.WebApp.Models.StreamElements;

public class StreamElementsEventMessage
{
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
    public string? EventLevel { get; set; }
    public string? ChannelName { get; set; }
    public string? ChannelId { get; set; }
    public double? DonationAmount { get; set; }
    public string? DonationCurrency { get; set; }
    public string? DonorName { get; set; }
}