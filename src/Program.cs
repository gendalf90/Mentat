using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Mentat;

var builder = Host.CreateApplicationBuilder(args);

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
    .Configure<OllamaOptions>(opt =>
    {
        opt.OllamaUrl = builder.Configuration.GetValue<string>("OllamaUrl");
        opt.OllamaModel = builder.Configuration.GetValue<string>("OllamaModel");
        opt.OllamaKeepAlive = builder.Configuration.GetValue<string>("OllamaKeepAlive");
    })
    .Configure<MailboxOptions>(opt =>
    {
        opt.ImapHost = builder.Configuration.GetValue<string>("MailImapHost");
        opt.ImapPort = builder.Configuration.GetValue<int>("MailImapPort");
        opt.SmtpHost = builder.Configuration.GetValue<string>("MailSmtpHost");
        opt.SmtpPort = builder.Configuration.GetValue<int>("MailSmtpPort");
        opt.Users = builder.Configuration.GetSection("MailUsers").Get<string[]>();
        opt.Login = builder.Configuration.GetValue<string>("MailLogin");
        opt.Password = builder.Configuration.GetValue<string>("MailPassword");
    });

builder.Services.AddHttpClient<OllamaClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<OllamaOptions>>();

    client.Timeout = Timeout.InfiniteTimeSpan;
    client.BaseAddress = new Uri(options.Value.OllamaUrl);
});

builder.Services
    .AddHostedService<MessageProcessor>()
    .AddScoped<Mailbox>()
    .AddScoped<Ollama>();

var host = builder.Build();

await host.RunAsync();
