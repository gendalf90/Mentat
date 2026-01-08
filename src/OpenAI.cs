using Microsoft.Extensions.Options;

namespace Mentat;

internal class OpenAIOptions
{
    public string OpenAIUrl { get; set; }

    public string OpenAIModel { get; set; }
}

internal class Message
{
    public string Text { get; set; }

    public bool FromBot { get; set; }
}

internal class OpenAI(OpenAIClient client, IOptions<OpenAIOptions> options)
{
    private const string AssistantRole = "assistant";
    private const string UserRole = "user";

    public async Task<Message> GetAnswer(IEnumerable<Message> chat, CancellationToken token = default)
    {
        var chatMessages = chat
            .Select(message => new OpenAIMessage
            {
                Role = message.FromBot ? AssistantRole : UserRole,
                Content = message.Text
            })
            .ToArray();

        var response = await client.Chat(new OpenAIChat
        {
            Messages = chatMessages,
            Stream = false,
            Model = options.Value.OpenAIModel
        }, token);

        return new Message
        {
            Text = response.Choices[0].Message.Content,
            FromBot = true
        };
    }
}