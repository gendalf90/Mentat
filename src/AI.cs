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
        var response = await client.GetResponseAsync(Map(chat), cancellationToken: token);

        return new Message
        {
            Text = response.Text,
            FromBot = true
        };
    }

    private IEnumerable<ChatMessage> Map(IEnumerable<Message> messages)
    {
        var prompt = options.Value.OpenAIPrompt;

        if (!string.IsNullOrEmpty(prompt))
        {
            yield return new ChatMessage(ChatRole.System, prompt);
        }

        foreach (var message in messages)
        {
            var role = message.FromBot ? ChatRole.Assistant : ChatRole.User;
        
            yield return new ChatMessage(role, message.Text);
        }
    }
}
