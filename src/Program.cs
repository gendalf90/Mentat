using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Mentat;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(builder => builder
    .ClearProviders()
    .AddSimpleConsole(opt => 
    {
        opt.SingleLine = true;
        opt.UseUtcTimestamp = true;
        opt.IncludeScopes = true;
        opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
    })
    .SetMinimumLevel(LogLevel.Information));

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services
    .Configure<MessageProcessorOptions>(opt =>
    {
        opt.PollInterval = builder.Configuration.GetValue<TimeSpan>("PollInterval");
    })
    .Configure<AIOptions>(opt =>
    {
        opt.OpenAIUrl = builder.Configuration.GetValue<string>("OpenAIUrl");
        opt.OpenAIModel = builder.Configuration.GetValue<string>("OpenAIModel");
        opt.OpenAIApiKey = builder.Configuration.GetValue<string>("OpenAIApiKey");
        opt.OpenAIPrompt = builder.Configuration.GetValue<string>("OpenAIPrompt");
    })
    .Configure<MailboxOptions>(opt =>
    {
        opt.ImapHost = builder.Configuration.GetValue<string>("MailImapHost");
        opt.ImapPort = builder.Configuration.GetValue<int>("MailImapPort");
        opt.SmtpHost = builder.Configuration.GetValue<string>("MailSmtpHost");
        opt.SmtpPort = builder.Configuration.GetValue<int>("MailSmtpPort");
        opt.Users = builder.Configuration
            .GetValue<string>("MailUsers")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        opt.Login = builder.Configuration.GetValue<string>("MailLogin");
        opt.Password = builder.Configuration.GetValue<string>("MailPassword");
    });

builder.Services
    .AddHostedService<MessageProcessor>()
    .AddScoped<Mailbox>()
    .AddScoped<AI>();

builder.Services.AddChatClient(provider =>
{
    var options = provider.GetRequiredService<IOptions<AIOptions>>();
    
    return new ChatClient(
        options.Value.OpenAIModel, 
        new ApiKeyCredential(options.Value.OpenAIApiKey), 
        new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Value.OpenAIUrl),
            NetworkTimeout = Timeout.InfiniteTimeSpan
        }).AsIChatClient();
}, ServiceLifetime.Scoped).UseLogging();

var host = builder.Build();

host.Run();
