using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Mentat;

public class OllamaChatRequest
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

public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage Message { get; set; }
}

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public OllamaModel[] Models { get; set; }
}

public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class OllamaOptions
{
    public string OllamaUrl { get; set; }
}

public class OllamaClient(HttpClient client)
{
    public async Task<OllamaChatResponse> Chat(OllamaChatRequest chat, CancellationToken token)
    {
        var response = await client.PostAsJsonAsync("api/chat", chat, token);

        return await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<OllamaChatResponse>(token);
    }
}