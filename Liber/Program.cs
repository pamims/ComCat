using System.Threading.Tasks;
using ComCat.Services;
using System;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Reflection;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.IO;
using ComCat.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ComCat.Services.Ballot;
using System.Net.Http;

namespace ComCat
{
    public class Program
    {        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to Liber! v0.1.2");
            ConfigService config = await ConfigService.GetAsync("config.json");
            
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = config.MessageCacheSize,
                GatewayIntents = GatewayIntents.All
            });
            var commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true,
                DefaultRunMode = RunMode.Async
            });
            var services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton(config)
                .AddDbContext<LDbContext>(options =>
                {
                    var folder = Environment.SpecialFolder.ApplicationData;
                    var path = Environment.GetFolderPath(folder);
                    options.UseSqlite($"Data Source={path}"
                        + $"{Path.DirectorySeparatorChar}Liber.db");
                })
                .AddSingleton<HttpClient>()
                .AddSingleton<LogService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<PassiveService>()
                .AddSingleton<BallotService>()
                .BuildServiceProvider();
            services.GetRequiredService<PassiveService>();
            services.GetRequiredService<LogService>();
            services.GetRequiredService<CommandHandler>();
            services.GetRequiredService<BallotService>();

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            await Task.Delay(Timeout.Infinite);
        }
    }
}
