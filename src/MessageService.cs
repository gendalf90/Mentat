using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentat;

public class MessageServiceOptions
{
    public TimeSpan PollInterval { get; set; }

    public string[] AllowedUsers { get; set; }

    public string OllamaModel { get; set; }

    public string OllamaKeepAlive { get; set; }
}

public class MessageService(
    TelegramClient telegramClient,
    OllamaClient ollamaClient,
    LocalStorage localStorage,
    IOptions<MessageServiceOptions> options,
    ILogger<MessageService> logger) : BackgroundService
{
    private const string ClearCommand = "/clear";
    private const string AssistantRole = "assistant";
    private const string UserRole = "user";
    private const string ResponseFormat = "markdown";
    private const int MaxAnswerMessageSize = 1024;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessMessages(stoppingToken);
        }
    }

    private async Task ProcessMessages(CancellationToken token)
    {
        try
        {
            var updates = await GetUpdates(token);

            if (updates == null)
            {
                return;
            }

            var messages = await CommitMessages(updates, token);
            
            await SetResponses(messages, token);
            await ClearMessages(messages, token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while processing new updates");
        }
    }

    private async Task ClearMessages(StorageMessages messages, CancellationToken token)
    {
        var clearCommands = messages.Log
            .Where(message => message.IsClearCommand)
            .GroupBy(message => message.ChatId)
            .Select(group => new
            {
                ChatId = group.Key,
                LastMessageId = group.Max(message => message.MessageId)
            })
            .ToList();

        if (clearCommands.Count == 0)
        {
            return;
        }

        foreach (var command in clearCommands)
        {
            messages.Log.RemoveAll(message => message.ChatId == command.ChatId && message.MessageId <= command.LastMessageId);
        }

        await localStorage.Save(messages, token);
    }

    private async Task SetResponses(StorageMessages messages, CancellationToken token)
    {
        var responses = new List<TelegramMessage>();

        foreach (var chat in messages.Log.GroupBy(message => message.ChatId))
        {
            if (chat.Any(message => message.IsNew && !message.IsClearCommand))
            {
                foreach (var batch in chat.OrderBy(message => message.MessageId).SplitBy(message => message.IsClearCommand))
                {
                    responses.AddRange(await SendAnswers(chat.Key, batch, token));
                }
            }
        }

        messages.Log.AddRange(responses.Select(MapNewStorageMessage));

        await localStorage.Save(messages, token);
    }

    private async Task<IEnumerable<TelegramMessage>> SendAnswers(ulong chatId, IEnumerable<StorageMessage> messages, CancellationToken token)
    {
        var chatMessages = messages
            .Select(message => new OllamaMessage
            {
                Role = message.IsBot ? AssistantRole : UserRole,
                Content = message.Text
            })
            .ToArray();

        var response = await ollamaClient.Chat(new OllamaChatRequest
        {
            Messages = chatMessages,
            Stream = false,
            Model = options.Value.OllamaModel,
            KeepAlive = options.Value.OllamaKeepAlive
        }, token);

        var answers = new List<TelegramMessage>();

        foreach (var content in response.Message.Content.SplitByParagraphs(MaxAnswerMessageSize))
        {
            var answer = await telegramClient.Answer(new TelegramAnswer
            {
                ChatId = chatId,
                Text = content,
                Format = ResponseFormat
            }, token);

            answers.Add(answer.Result);
        }

        return answers;
    }

    private StorageMessages UpdateMessages(StorageMessages messages, TelegramUpdates updates)
    {
        if (messages == null)
        {
            return new StorageMessages
            {
                Offset = updates.Result.Max(result => result.Offset),
                Log = updates.Result.Select(update => MapNewStorageMessage(update.Message)).ToList()
            };
        }

        return new StorageMessages
        {
            Offset = updates.Result.Max(result => result.Offset),
            Log = updates.Result
                .Select(update => MapNewStorageMessage(update.Message))
                .Concat(messages.Log)
                .DistinctBy(message => message.MessageId)
                .OrderBy(message => message.MessageId)
                .ToList()
        };
    }

    private async Task<TelegramUpdates> GetUpdates(CancellationToken token)
    {
        var updates = await telegramClient.GetUpdates(token);

        if (updates.Result.Length == 0)
        {
            return null;
        }

        var accepted = updates.Result
            .Where(update => IsAcceptable(update.Message))
            .ToArray();

        if (accepted.Length == 0)
        {
            await telegramClient.Commit(updates.Result.Max(update => update.Offset) + 1, token);

            return null;
        }

        updates.Result = accepted;

        return updates;
    }

    private async Task<StorageMessages> CommitMessages(TelegramUpdates updates, CancellationToken token)
    {
        var current = await localStorage.Load(token);

        var updated = UpdateMessages(current, updates);

        await localStorage.Save(updated, token);
        await telegramClient.Commit(updated.Offset + 1, token);

        return updated;
    }

    private bool IsAcceptable(TelegramMessage message)
    {
        return !message.From.IsBot && options.Value.AllowedUsers.Contains(message.From.Username);
    }

    private StorageMessage MapNewStorageMessage(TelegramMessage message)
    {
        return new StorageMessage
        {
            MessageId = message.MessageId,
            ChatId = message.Chat.Id,
            Text = message.Text,
            IsBot = message.From.IsBot,
            IsClearCommand = message.Text == ClearCommand,
            IsNew = true
        };
    }
}