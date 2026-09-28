using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Mentat;
using System.ClientModel;
using OpenAI;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Formatting.Compact;
using OpenAI.Responses;

var builder = Host.CreateApplicationBuilder(args);
var logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger, true);

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

    #pragma warning disable OPENAI001
    return new ResponsesClient(
        new ApiKeyCredential(options.Value.OpenAIApiKey), 
        new ResponsesClientOptions
        {
            Endpoint = new Uri(new Uri(options.Value.OpenAIUrl), "v1/"),
            NetworkTimeout = Timeout.InfiniteTimeSpan
        }).AsIChatClient(options.Value.OpenAIModel);
    #pragma warning restore OPENAI001
}, ServiceLifetime.Scoped).UseLogging();

var host = builder.Build();

host.Run();
