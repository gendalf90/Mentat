using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Mentat;

internal class OllamaChatRequest
{
    [JsonPropertyName("messages")]
    public OllamaMessage[] Messages { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("keep_alive")]
    public string KeepAlive { get; set; }
}

internal class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

internal class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage Message { get; set; }
}

internal class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public OllamaModel[] Models { get; set; }
}

internal class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

internal class OllamaClient(HttpClient client)
{
    public async Task<OllamaChatResponse> Chat(OllamaChatRequest chat, CancellationToken token = default)
    {
        var response = await client.PostAsJsonAsync("api/chat", chat, token);

        return await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<OllamaChatResponse>(token);
    }
}