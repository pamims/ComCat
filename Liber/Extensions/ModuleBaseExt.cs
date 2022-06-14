using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ComCat.Extensions
{
    static class ModuleBaseExt
    {
        public static async Task CleanUp(this ModuleBase module, ICommandContext context, IUserMessage message = null)
        {
            await Task.Delay(2500);
            if (message != null)
                await context.Channel.DeleteMessageAsync(message);
            await context.Channel.DeleteMessageAsync(context.Message);
        }

        public static void ConsoleWrite(this ModuleBase module, string reply, ICommandContext context)
        {
            Console.WriteLine($"{context.Channel.Name} >> {context.Client.CurrentUser}: "
                + $"'{reply}'");
        }
    }
}
