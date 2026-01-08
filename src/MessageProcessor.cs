using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentat;

public class MessageProcessorOptions
{
    public TimeSpan PollInterval { get; set; }
}

public class MessageProcessor(
    IServiceProvider provider,
    IOptions<MessageProcessorOptions> options,
    ILogger<MessageProcessor> logger) : BackgroundService
{   
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.WhenAll(ProcessMessages(stoppingToken), Task.Delay(options.Value.PollInterval, stoppingToken));
        }
    }

    private async Task ProcessMessages(CancellationToken token)
    {
        using var scope = provider.CreateScope();
        
        var mailbox = scope.ServiceProvider.GetRequiredService<Mailbox>();
        var openAI = scope.ServiceProvider.GetRequiredService<OpenAI>();
        
        try
        {
            var thread = await mailbox.GetThreadToAnswer(token);

            if (!thread.Any())
            {
                logger.LogInformation("No messages for processing");
                
                return;
            }

            var ordered = thread.OrderBy(message => message.Date);

            var response = await openAI.GetAnswer(ordered.Select(message => new Message
            {
                Text = message.Text,
                FromBot = message.FromBot
            }), token);

            var lastMessage = ordered.Last();

            await mailbox.SendAnswerToMessage(lastMessage.Id, response.Text, token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while processing messages");
        }
    }
}