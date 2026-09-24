using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Mentat;

internal class AIOptions
{
    public string OpenAIUrl { get; set; }

    public string OpenAIModel { get; set; }

    public string OpenAIApiKey { get; set; }

    public string OpenAIPrompt { get; set; }
}

internal class Message
{
    public string Text { get; set; }

    public bool FromBot { get; set; }
}

internal class AI(IChatClient client, IOptions<AIOptions> options)
{
    public async Task<Message> GetAnswer(IEnumerable<Message> chat, CancellationToken token = default)
    {
        var response = await client.GetResponseAsync(chat.Select(Map), new ChatOptions
        {
            Instructions = options.Value.OpenAIPrompt
        }, token);

        return new Message
        {
            Text = response.Text,
            FromBot = true
        };
    }

    private ChatMessage Map(Message message)
    {
        return new ChatMessage(message.FromBot ? ChatRole.Assistant : ChatRole.User, message.Text);
    }
}
