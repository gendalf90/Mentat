using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Mentat;

internal class OpenAIChat
{
    [JsonPropertyName("messages")]
    public OpenAIMessage[] Messages { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }
}

internal class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

internal class OpenAIReply
{
    [JsonPropertyName("choices")]
    public OpenAIChoice[] Choices { get; set; }
}

internal class OpenAIChoice
{
    [JsonPropertyName("message")]
    public OpenAIMessage Message { get; set; }
}

internal class OpenAIClient(HttpClient client)
{
    public async Task<OpenAIReply> Chat(OpenAIChat chat, CancellationToken token = default)
    {
        var response = await client.PostAsJsonAsync("v1/chat/completions", chat, token);

        return await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<OpenAIReply>(token);
    }
}