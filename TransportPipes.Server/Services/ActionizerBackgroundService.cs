using Microsoft.Extensions.Options;
using TransportPipes.Actionizer;

namespace TransportPipes.Server.Services;

/// <summary>
/// Runs the Actionizer (this used to be a separate console application) and sends the resulting updates to the clients
/// </summary>
public class ActionizerBackgroundService : BackgroundService
{
    private readonly TransportActionizer _actionizer;
    private readonly ClientUpdateDispatcher _clientUpdateDispatcher;
    private readonly TransportPipesOptions _options;
    private readonly ILogger<ActionizerBackgroundService> _logger;

    public ActionizerBackgroundService(TransportActionizer actionizer, ClientUpdateDispatcher clientUpdateDispatcher, IOptions<TransportPipesOptions> options, ILogger<ActionizerBackgroundService> logger)
    {
        _actionizer = actionizer;
        _clientUpdateDispatcher = clientUpdateDispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started Actionizer (every {Interval}s)", _options.ActionizerIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ActionizerIntervalSeconds));
        try
        {
            do
            {
                await RunOnceAsync();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Actionizer shutting down...");
    }

    internal async Task RunOnceAsync()
    {
        try
        {
            _actionizer.ExecuteAllOpenActions(DateTime.UtcNow);
            await _clientUpdateDispatcher.DispatchPendingClientUpdatesAsync();
        }
        catch (Exception ex)
        {
            //Keep the game running, the actions will be retried on the next tick
            _logger.LogError(ex, "Error while executing actions");
        }
    }
}
