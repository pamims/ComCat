using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace ComCat.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private static string _prefix;

        public CommandHandler(IServiceProvider provider,
            DiscordSocketClient client, CommandService commands, ConfigService config)
        {
            _provider = provider;
            _commands = commands;
            _commands.CommandExecuted += OnCommandExecuted;
            _prefix = config.Prefix;
            _client = client;
            _client.MessageReceived += HandleCommands;
        }

        private async Task HandleCommands(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            if (!(arg is SocketUserMessage msg)) return;
            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;
            int pos = 0;
            if (!msg.HasStringPrefix($"{_prefix} ", ref pos) 
                && !msg.HasStringPrefix(_prefix, ref pos))
                return;
            var context = new SocketCommandContext(_client, msg);
            await _commands.ExecuteAsync(context, pos, _provider);
        }

        private async Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (result.IsSuccess) return;
            var reason = $"command failed → {result.ErrorReason.ToLower()}";
            var message = await context.Channel.SendMessageAsync(reason);
            await Task.Delay(5000);
            await context.Channel.DeleteMessageAsync(message);
            await context.Channel.DeleteMessageAsync(context.Message);
        }
    }
}
