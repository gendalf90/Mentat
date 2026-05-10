using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

namespace Mentat;

internal class AIOptions
{
    public string OpenAIUrl { get; set; }

    public string OpenAIModel { get; set; }

    public string OpenAIApiKey { get; set; }
}

internal class Message
{
    public string Text { get; set; }

    public bool FromBot { get; set; }
}

#pragma warning disable OPENAI001
internal class AI(IOptions<AIOptions> options)
{
    public async Task<Message> GetAnswer(IEnumerable<Message> chat, CancellationToken token = default)
    {
        var client = new OpenAIClient(new ApiKeyCredential(options.Value.OpenAIApiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Value.OpenAIUrl),
            NetworkTimeout = Timeout.InfiniteTimeSpan
        });
        
        var responses = client.GetResponsesClient();
        var request = new CreateResponseOptions(options.Value.OpenAIModel, chat.Select(Map))
        {
            StoredOutputEnabled = false
        };

        var response = await responses.CreateResponseAsync(request, token);

        return new Message
        {
            Text = response.Value.GetOutputText(),
            FromBot = true
        };
    }

    private ResponseItem Map(Message message)
    {
        var parts = new List<ResponseContentPart>
        {
            ResponseContentPart.CreateInputTextPart(message.Text)
        };

        return message.FromBot
            ? ResponseItem.CreateAssistantMessageItem(parts)
            : ResponseItem.CreateUserMessageItem(parts);
    }
}
#pragma warning restore OPENAI001