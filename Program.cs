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

builder.Services.Configure<TelegramOptions>(opt =>
{
    opt.BotToken = builder.Configuration[nameof(opt.BotToken)];
    opt.TelegramUrl = builder.Configuration[nameof(opt.TelegramUrl)];
});
builder.Services.Configure<StorageOptions>(opt =>
{
    opt.StorageFolder = builder.Configuration[nameof(opt.StorageFolder)];
});
builder.Services.Configure<MessageServiceOptions>(opt =>
{
    opt.PollInterval = TimeSpan.Parse(builder.Configuration[nameof(opt.PollInterval)]);
    opt.AllowedUsers = builder.Configuration[nameof(opt.AllowedUsers)].Split(',');
    opt.OllamaModel = builder.Configuration[nameof(opt.OllamaModel)];
    opt.OllamaKeepAlive = builder.Configuration[nameof(opt.OllamaKeepAlive)];
});
builder.Services.Configure<OllamaOptions>(opt =>
{
    opt.OllamaUrl = builder.Configuration[nameof(opt.OllamaUrl)];
});

builder.Services.AddHttpClient<TelegramClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<TelegramOptions>>();

    client.BaseAddress = new Uri($"{options.Value.TelegramUrl.TrimEnd('/')}/bot{options.Value.BotToken}/");
});
builder.Services.AddHttpClient<OllamaClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<OllamaOptions>>();

    client.Timeout = Timeout.InfiniteTimeSpan;
    client.BaseAddress = new Uri(options.Value.OllamaUrl);
});
builder.Services.AddSingleton<LocalStorage>();
builder.Services.AddHostedService<MessageService>();

var host = builder.Build();

await host.RunAsync();
