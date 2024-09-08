using Shared.Models;

namespace Tx.Services;
public class InputService
{
    private readonly ILogger<InputService> _logger;

    public InputService(ILogger<InputService> logger)
    {
        _logger = logger;
    }

    public Channel ReadInputs(InType inType)
    {
        _logger.LogInformation($"Reading inputs from {inType}");
        return new Channel { Value = [1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000] };
    }
}
