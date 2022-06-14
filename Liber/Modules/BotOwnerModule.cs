using Discord.Commands;
using Discord.WebSocket;
using ComCat.Extensions;
using ComCat.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Discord;

namespace ComCat.Modules
{
    public class BotOwnerModule : ModuleBase
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BotOwnerModule(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        [Command("wipePoints")]
        [Summary("removes all points from a user - owner command only")]
        [RequireOwner]
        public async Task WipePoints(IGuildUser user)
        {
            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var member = await db.GetServerMemberAsync(Context.Guild.Id, user.Id);
            member.Points = 0;
            await db.SaveChangesAsync();
            var message = await Context.Channel.SendMessageAsync("wiped points");
            await this.CleanUp(Context, message);
        }

        [Command("wipedb")]
        [Summary("cleans up the database - owner command only")]
        [RequireOwner]
        public async Task WipeDb()
        {
            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var dbContext);
            await dbContext.ClearTablesAsync();
            var message = await Context.Channel.SendMessageAsync("wiped database");
            await this.CleanUp(Context, message);
        }

        [Command("givePosts")]
        [Summary("grants posts to a user - owner command only")]
        [RequireOwner]
        public async Task GivePosts(uint numPosts, IGuildUser user = null)
        {
            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var dbContext);
            user ??= (Context.User as IGuildUser);
            var serverMember = await dbContext.GetServerMemberAsync(user.GuildId, user.Id);
            serverMember.NumPosts += numPosts;
            await dbContext.SaveChangesAsync();
            var reply = $"Gave {user.Username} {numPosts} posts for a total of "
                + $"{serverMember.NumPosts} posts today.";
            var message = await Context.Channel.SendMessageAsync(reply);
            await this.CleanUp(Context, message);
        }

        [Command("simNewDay")]
        [Summary("simulate a new day in the database - owner command only")]
        [RequireOwner]
        public async Task SimNewDay()
        {
            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var dbContext);
            await dbContext.UpdateServerMembersAsync();
            var message = await Context.Channel.SendMessageAsync("simulated a new day");
            await this.CleanUp(Context, message);
        }
    }
}
