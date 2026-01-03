using Microsoft.Extensions.Options;

namespace Mentat;

internal class OllamaOptions
{
    public string OllamaUrl { get; set; }

    public string OllamaModel { get; set; }

    public string OllamaKeepAlive { get; set; }
}

internal class Message
{
    public string Text { get; set; }

    public bool FromBot { get; set; }
}

internal class Ollama(OllamaClient client, IOptions<OllamaOptions> options)
{
    private const string AssistantRole = "assistant";
    private const string UserRole = "user";

    public async Task<Message> GetAnswer(IEnumerable<Message> chat, CancellationToken token = default)
    {
        var chatMessages = chat
            .Select(message => new OllamaMessage
            {
                Role = message.FromBot ? AssistantRole : UserRole,
                Content = message.Text
            })
            .ToArray();

        var response = await client.Chat(new OllamaChatRequest
        {
            Messages = chatMessages,
            Stream = false,
            Model = options.Value.OllamaModel,
            KeepAlive = options.Value.OllamaKeepAlive
        }, token);

        return new Message
        {
            Text = response.Message.Content,
            FromBot = true
        };
    }
}