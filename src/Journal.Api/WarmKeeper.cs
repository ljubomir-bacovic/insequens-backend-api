public class WarmKeeper : BackgroundService
{
    private readonly ILogger<WarmKeeper> _logger;
    private readonly IHttpClientFactory _clientFactory;

    public WarmKeeper(ILogger<WarmKeeper> logger, IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = _clientFactory.CreateClient();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await client.GetAsync("https://www.insequens.com:5000/warmup", stoppingToken);
                _logger.LogInformation("Warmup ping response: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Warmup ping failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
