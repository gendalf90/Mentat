using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using Markdig;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

internal class Email
{   
    public string Id { get; set; }

    public bool FromBot { get; set; }
    
    public string Text { get; set; }

    public DateTime Date { get; set; }
}

internal class MailboxOptions
{
    public string ImapHost { get; set; }

    public int ImapPort { get; set; }

    public string SmtpHost { get; set; }

    public int SmtpPort { get; set; }

    public string Login { get; set; }

    public string Password { get; set; }

    public string[] Users { get; set; }
}

internal class Mailbox(IOptions<MailboxOptions> options, ILogger<Mailbox> logger)
{
    private const string Name = "Mentat";
    
    public async Task<IEnumerable<Email>> GetThreadToAnswer(CancellationToken token = default)
    {
        using var client = new ImapClient();

        await InitClient(client, token);
        
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly, token);

        var allUids = await client.Inbox.SearchAsync(SearchQuery.All, token);

        logger.LogInformation($"Found {allUids.Count} messages at server");

        var summaryFlags = MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.References;

        var allSummaries = await client.Inbox.FetchAsync(allUids, summaryFlags, token);

        var earliestNotAnsweredSummary = allSummaries
            .Where(summary => summary.Envelope.From.Mailboxes.Any(mailbox => options.Value.Users.Any(user => mailbox.Address.Equals(user, StringComparison.OrdinalIgnoreCase))))
            .Where(summary => !summary.Flags.Value.HasFlag(MessageFlags.Answered))
            .OrderBy(summary => summary.Envelope.Date)
            .FirstOrDefault();

        if (earliestNotAnsweredSummary == null)
        {
            return [];
        }

        var threadSummaries = earliestNotAnsweredSummary.References
            .Join(allSummaries, messageId => messageId, summary => summary.Envelope.MessageId, (messageId, summary) => summary, StringComparer.OrdinalIgnoreCase)
            .Append(earliestNotAnsweredSummary)
            .DistinctBy(summary => summary.UniqueId);

        var threadMessages = new List<Email>();

        foreach (var summary in threadSummaries)
        {
            threadMessages.Add(Map(await client.Inbox.GetMessageAsync(summary.UniqueId, token)));
        }

        logger.LogInformation($"Found thread with {threadMessages.Count} messages for request {earliestNotAnsweredSummary.Envelope.MessageId} from {earliestNotAnsweredSummary.Envelope.From}");

        return threadMessages;
    }

    public async Task SendAnswerToMessage(string messageId, string answerText, CancellationToken token = default)
    {
        using var client = new ImapClient();

        await InitClient(client, token);

        await client.Inbox.OpenAsync(FolderAccess.ReadWrite, token);

        var allUids = await client.Inbox.SearchAsync(SearchQuery.All, token);

        var summaryFlags = MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.References;

        var allSummaries = await client.Inbox.FetchAsync(allUids, summaryFlags, token);

        var answeringMessageSummary = allSummaries.FirstOrDefault(summary => summary.Envelope.MessageId == messageId);

        if (answeringMessageSummary == null)
        {
            return;
        }

        var reply = new MimeMessage();

        reply.To.AddRange(answeringMessageSummary.Envelope.From.Mailboxes.Where(mailbox => options.Value.Users.Any(user => mailbox.Address.Equals(user, StringComparison.OrdinalIgnoreCase))));
        reply.From.Add(new MailboxAddress(Name, options.Value.Login));
        reply.Bcc.Add(new MailboxAddress(Name, options.Value.Login));
        reply.Subject = answeringMessageSummary.Envelope.Subject.Contains("Re:", StringComparison.OrdinalIgnoreCase)
            ? answeringMessageSummary.Envelope.Subject
            : $"Re: {answeringMessageSummary.Envelope.Subject}";
        reply.InReplyTo = answeringMessageSummary.Envelope.MessageId;
        reply.References.AddRange(answeringMessageSummary.References);
        reply.References.Add(answeringMessageSummary.Envelope.MessageId);
        reply.Body = BuildBody(answerText);

        await SendMessage(reply, token);
        
        await client.Inbox.SetFlagsAsync(answeringMessageSummary.UniqueId, MessageFlags.Answered, true, token);
    }

    private async Task InitClient(ImapClient client, CancellationToken token)
    {
        await client.ConnectAsync(options.Value.ImapHost, options.Value.ImapPort, cancellationToken: token);
        await client.AuthenticateAsync(options.Value.Login, options.Value.Password, token);

        logger.LogInformation($"Authentificated as {options.Value.Login} in Imap: {options.Value.ImapHost}:{options.Value.ImapPort}");
    }

    private async Task SendMessage(MimeMessage message, CancellationToken token)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(options.Value.SmtpHost, options.Value.SmtpPort, cancellationToken: token);
        await client.AuthenticateAsync(options.Value.Login, options.Value.Password, token);

        logger.LogInformation($"Authentificated as {options.Value.Login} in Smtp: {options.Value.SmtpHost}:{options.Value.SmtpPort}");

        await client.SendAsync(message, token);

        logger.LogInformation($"Message with subject {message.Subject} is sent to {message.To} as reply to message {message.InReplyTo}");
    }

    private static MimeEntity BuildBody(string text)
    {
        var builder = new BodyBuilder();

        builder.TextBody = text;
        builder.HtmlBody = Markdown.ToHtml(text, new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build());

        return builder.ToMessageBody();
    }

    private Email Map(MimeMessage message)
    {
        var isBot = message.From.Mailboxes.Any(mailbox => mailbox.Address.Equals(options.Value.Login, StringComparison.OrdinalIgnoreCase));
        var text = message.TextBody ?? message.HtmlBody ?? string.Empty;

        return new Email
        {
            Id = message.MessageId,
            FromBot = isBot,
            Text = text,
            Date = message.Date.UtcDateTime
        };
    }
}