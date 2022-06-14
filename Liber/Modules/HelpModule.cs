using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComCat.Modules
{
    public class HelpModule : ModuleBase
    {
        private readonly CommandService _commands;

        public HelpModule(CommandService commands)
            : base()
        {
            _commands = commands;
        }

        [Command("help")]
        [Summary("lists commands that are available to the user")]
        public async Task Help()
        {
            List<CommandInfo> commands = await GetUsableCommandsAsync(Context)
                .OrderBy<CommandInfo, string>(c => c.Name)
                .OrderBy<CommandInfo, string>(c => c.Module.Name)
                .ToListAsync<CommandInfo>();

            foreach (ModuleInfo module in _commands.Modules)
            {
                string moduleName = module.Name;
                List<CommandInfo> moduleCommands = commands
                    .Where(c => c.Module.Name == moduleName)
                    .ToList();
                if (moduleCommands.Count < 1) continue;
                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithTitle($"{moduleName}")
                    .WithColor(Color.LightOrange);
                foreach (CommandInfo command in moduleCommands)
                {
                    string embedFieldText = command.Summary ?? "No description available\n";
                    embedBuilder.AddField(command.Name, embedFieldText);
                }
                await ReplyAsync(embed: embedBuilder.Build());
            }
        }

        private async IAsyncEnumerable<CommandInfo> GetUsableCommandsAsync(ICommandContext context)
        {
            foreach (CommandInfo command in _commands.Commands)
            {
                PreconditionResult result = await command.CheckPreconditionsAsync(context);
                if (result.IsSuccess)
                    yield return command;
            }
        }

    }
}
