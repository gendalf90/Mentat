using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Mentat;

public class StorageMessages
{
    public ulong Offset { get; set; }

    public List<StorageMessage> Log { get; set; }
}

public class StorageMessage
{
    public ulong MessageId { get; set; }

    public ulong ChatId { get; set; }

    public string Text { get; set; }

    public bool IsBot { get; set; }

    public bool IsClearCommand { get; set; }

    public bool IsNew { get; set; }
}

public class StorageOptions
{
    public string StorageFolder { get; set; }
}

public class LocalStorage(IOptions<StorageOptions> options)
{
    private const string LogFileName = "messages_log";

    private static JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task Save(StorageMessages messages, CancellationToken token = default)
    {
        var logFullPath = Path.Combine(options.Value.StorageFolder, LogFileName);
        var logData = new Messages(messages.Offset, messages.Log.Select(ToMessage).ToList());
        var content = JsonSerializer.Serialize(logData, serializerOptions);

        await File.WriteAllTextAsync(logFullPath, content, token);
    }

    private Message ToMessage(StorageMessage message)
    {
        return new Message
        (
            message.MessageId,
            message.ChatId,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Text)),
            message.IsBot,
            message.IsClearCommand
        );
    }

    public async Task<StorageMessages> Load(CancellationToken token = default)
    {
        var logFullPath = Path.Combine(options.Value.StorageFolder, LogFileName);

        if (!File.Exists(logFullPath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(logFullPath, token);
        var logData = JsonSerializer.Deserialize<Messages>(content);

        return new StorageMessages
        {
            Offset = logData.Offset,
            Log = logData.Log.Select(FromMessage).ToList()
        };
    }

    private StorageMessage FromMessage(Message message)
    {
        return new StorageMessage
        {
            MessageId = message.MessageId,
            ChatId = message.ChatId,
            Text = Encoding.UTF8.GetString(Convert.FromBase64String(message.Data)),
            IsBot = message.IsBot,
            IsClearCommand = message.IsClearCommand
        };
    }

    private record Messages(ulong Offset, IReadOnlyList<Message> Log);

    private record Message(ulong MessageId, ulong ChatId, string Data, bool IsBot, bool IsClearCommand);
}