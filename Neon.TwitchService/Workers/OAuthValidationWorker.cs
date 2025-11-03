using Coravel.Invocable;
using Neon.TwitchService.Services.OAuthValidations;

namespace Neon.TwitchService.Workers;

public class OAuthValidationWorker(ILogger<OAuthValidationWorker> logger, IServiceScopeFactory serviceScopeFactory) : ICancellableInvocable
{
    private bool _firstCheck = true;
    private const int FirstDelay = 30000;

    public CancellationToken CancellationToken { get; set; }
    
    public async Task InvokeAsync()
    {
        CancellationToken.ThrowIfCancellationRequested();

        //bypassing the first run to allow services to start
        if (_firstCheck)
        {
            logger.LogInformation("Bypassing first oauth validation for {Delay}ms to allow services to start.", FirstDelay);
            _firstCheck = false;
            await Task.Delay(FirstDelay, CancellationToken);
        }
        
        using var scope = serviceScopeFactory.CreateScope();
        var oauthValidationService = scope.ServiceProvider.GetRequiredService<IOAuthValidationService>();
        
        await oauthValidationService.ValidateAllUserTokensAsync(CancellationToken);
    }
}