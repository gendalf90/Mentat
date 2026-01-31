using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Mentat;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opt =>
{
    opt.SingleLine = true;
    opt.UseUtcTimestamp = true;
    opt.IncludeScopes = true;
    opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
});
builder.Logging.SetMinimumLevel(LogLevel.Information);

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
    .Configure<OpenAIOptions>(opt =>
    {
        opt.OpenAIUrl = builder.Configuration.GetValue<string>("OpenAIUrl");
        opt.OpenAIModel = builder.Configuration.GetValue<string>("OpenAIModel");
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

builder.Services.AddHttpClient<OpenAIClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<OpenAIOptions>>();

    client.Timeout = Timeout.InfiniteTimeSpan;
    client.BaseAddress = new Uri(options.Value.OpenAIUrl);
});

builder.Services
    .AddHostedService<MessageProcessor>()
    .AddScoped<Mailbox>()
    .AddScoped<OpenAI>();

var host = builder.Build();

await host.RunAsync();
