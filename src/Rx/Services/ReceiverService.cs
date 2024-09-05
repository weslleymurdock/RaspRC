using System.Threading;     
using Shared.Extensions;

namespace Rx.Services;

public class ReceiverService  
{
    private readonly ILogger<ReceiverService> _logger;

    public ReceiverService(ILogger<ReceiverService> logger) 
    {
        _logger = logger;
    }

    public async Task Update(string data)
    {
        //gpio update
        
        // some delay
        
        _logger.LogInformation($"GPIO Update with data:{ data }");
        
    }
    
    
}
