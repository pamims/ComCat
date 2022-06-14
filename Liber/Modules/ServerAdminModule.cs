using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ComCat.Extensions;
using ComCat.Services.Ballot;
using ComCat.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace ComCat.Modules
{
    public class ServerAdminModule : ModuleBase
    {

        private readonly IServiceScopeFactory _scopeFactory;

        public ServerAdminModule(IServiceScopeFactory scopeFactory)
            : base()
        {
            _scopeFactory = scopeFactory;
        }


        [Command("setCitizenRole")]
        [Summary("sets the role that marks citizen status - Admin only")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetCitizenRole(SocketRole citizen)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            server.CitizenRole = citizen.Id;
            await db.SaveChangesAsync();
            var message = await Context.Channel.SendMessageAsync($"citizenship role is set to {citizen.Name}");
            await this.CleanUp(Context, message);
        }

        [Command("giveGrace")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("extend a grace period to all server members.")]
        public async Task GiveGrace()
        {
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            await db.Entry(server).Collection(s => s.Members).LoadAsync();
            foreach (ServerMember member in server.Members)
            {
                if (member.SmoothMovingAverage < 0.2M)
                    member.SmoothMovingAverage = 0.2M;
            }
            await db.SaveChangesAsync();
            var message = await Context.Channel.SendMessageAsync("grace extended");
            await this.CleanUp(Context, message);
        }

        [Command("prune")]
        [Summary("prune members who show no activity")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Prune()
        {
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            await db.Entry(server).Collection(s => s.Members).LoadAsync();
            for (int i = server.Members.Count - 1; i >= 0; i--)
            {
                if (server.Members[i].SmoothMovingAverage < 0.2M)
                    db.Remove(server.Members[i]);
            }
            await db.SaveChangesAsync();

            var guildUsers = await Context.Guild.GetUsersAsync();
            foreach (IGuildUser user in guildUsers)
            {
                if (!server.Members.Any(m => m.MemberID == user.Id)
                    && !user.IsBot)
                {
                    Console.WriteLine($"trying to kick {user.Nickname}");
                    await user.KickAsync("kicked for inactivity");
                }
            }
            var message = await Context.Channel.SendMessageAsync("pruned members who show no activity");
            await this.CleanUp(Context, message);
        }
    }
}
