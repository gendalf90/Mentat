using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Mentat;

public class TelegramUpdates
{
    [JsonPropertyName("result")]
    public TelegramUpdate[] Result { get; set; }
}

public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public ulong Offset { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessage Message { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("from")]
    public TelegramUser From { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat Chat { get; set; }

    [JsonPropertyName("document")]
    public TelegramDocument Document { get; set; }
}

public class TelegramUser
{
    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }
}

public class TelegramChat
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }
}

public class TelegramAnswer
{
    [JsonPropertyName("chat_id")]
    public ulong ChatId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("parse_mode")]
    public string Format { get; set; }
}

public class TelegramResponse
{
    [JsonPropertyName("result")]
    public TelegramMessage Result { get; set; }
}

public class TelegramDocument
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }
}

public class TelegramOptions
{
    public string TelegramUrl { get; set; }

    public string BotToken { get; set; }
}

public class TelegramClient(HttpClient client)
{
    public async Task<TelegramUpdates> GetUpdates(CancellationToken token = default)
    {
        return await client.GetFromJsonAsync<TelegramUpdates>("getUpdates", token);
    }

    public async Task<TelegramResponse> Answer(TelegramAnswer answer, CancellationToken token = default)
    {
        var response = await client.PostAsJsonAsync("sendMessage", answer, token);

        return await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<TelegramResponse>(token);
    }

    public async Task Commit(ulong offset, CancellationToken token = default)
    {
        var response = await client.GetAsync($"getUpdates?offset={offset}", token);

        response.EnsureSuccessStatusCode();
    }
}
