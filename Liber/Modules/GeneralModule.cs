using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ComCat.Extensions;
using ComCat.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComCat.Modules
{
    public class GeneralModule : ModuleBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public GeneralModule(CommandService commands, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        // Remove this
        [Command("ping")]
        [Summary("see if the bot is out there")]
        public async Task Ping()
        {
            var reply = "echo";
            var message = await Context.Channel.SendMessageAsync(reply);
            await this.CleanUp(Context, message);
        }


        [Command("stats")]
        [Summary("see someone's server stats")]
        public async Task Stats(SocketGuildUser user = null)
        {
            using var scope = 
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            user ??= (Context.User as SocketGuildUser);
            var serverMember = await db.GetServerMemberAsync(user.Guild.Id, user.Id);
            var server = await db.GetServerAsync(user.Guild.Id);
            await db.Entry(server).Collection(s => s.Members).LoadAsync();
            var activityRank = server.Members
                .OrderByDescending<ServerMember, decimal>(m => m.SmoothMovingAverage)
                .TakeWhile(m => !m.Equals(serverMember))
                .Count()
                + 1;
            var pointsRank = server.Members
                .OrderByDescending<ServerMember, uint>(m => m.Points)
                .TakeWhile(m => !m.Equals(serverMember))
                .Count()
                + 1;
            var builder = new EmbedBuilder()
                .WithTitle($"{user.Username}#{user.Discriminator} Stats")
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .AddField("posts today :", serverMember.NumPosts)
                .AddField("_ _", "_ _")
                .AddField("activity score :", serverMember.SmoothMovingAverage, true)
                .AddField("activity rank : ", $"#{activityRank}", true)
                .AddField("_ _", "_ _")
                .AddField("points :", serverMember.Points, true)
                .AddField("points rank : ", $"#{pointsRank}", true)
                .WithColor(Color.LightOrange)
                ;
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }

        // combine with dbinfo
        [Command("whois")]
        [Summary("learn more about yourself or someone else")]
        public async Task WhoIs(SocketGuildUser user = null)
        {
            using var scope = 
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            user ??= (Context.User as SocketGuildUser);
            var serverMember = await db.GetServerMemberAsync(user.Guild.Id, user.Id);
            var roles = string.Join("\n", user.Roles
                .OrderBy(r => r.Position)
                .TakeLast(3)
                .Reverse()
                .Select(x => x.Mention));
            roles = roles.Length <= 1024 ? roles : roles.Substring(0, 1024);

            var builder = new EmbedBuilder()
                .WithThumbnailUrl(user.GetAvatarUrl()
                    ?? user.GetDefaultAvatarUrl())
                .WithTitle($"{user.Username}#{user.Discriminator}")
                .AddField("user id :", user.Id)
                .AddField("created :", user.CreatedAt.ToString("HH:mm:ss - dd/MM/yyyy"))
                .AddField("joined :", user.JoinedAt?.ToString("HH:mm:ss - dd/MM/yyyy"))
                .AddField("posts today :", serverMember.NumPosts)
                .AddField("activity rating :", serverMember.SmoothMovingAverage)
                .AddField("points :", serverMember.Points)
                .AddField("roles :", roles)
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                ;
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }

        [Command("leaderBoards")]
        [Alias("lb")]
        [Summary("shows the top 10 members by points and activity")]
        public async Task LeaderBoard()
        {
            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            await db.Entry<Server>(server).Collection(s => s.Members).LoadAsync();
            List<ulong> topTenActivity = server.Members
                .OrderByDescending<ServerMember, decimal>(m => m.SmoothMovingAverage)
                .Take<ServerMember>(10)
                .Select(m => m.MemberID)
                .ToList();
            List<ulong> topTenPoints = server.Members
                .OrderByDescending<ServerMember, uint>(m => m.Points)
                .Take<ServerMember>(10)
                .Select(m => m.MemberID)
                .ToList();
            string topTenActivityString = "";
            for (int i = 0; i < 10; i++)
            {
                var user = await Context.Guild.GetUserAsync(topTenActivity[i]);
                topTenActivityString += $"**#{i + 1}**\t"
                    + (user != null ? $"{user.Username}#{user.Discriminator}"
                    : "unknown")
                    +"\n";
            }
            string topTenPointsString = "";
            for (int i = 0; i < 10; i++)
            {
                var user = await Context.Guild.GetUserAsync(topTenPoints[i]);
                topTenPointsString += $"**#{i + 1}**\t"
                    + (user != null ? $"{user.Username}#{user.Discriminator}"
                    : "unknown")
                    + "\n";
            }
            var builder = new EmbedBuilder()
                .WithTitle("Leader Boards")
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithColor(Color.Orange)
                .AddField("__Top Ten By Activity__", topTenActivityString, true)
                .AddField("__Top Ten By Points__", topTenPointsString, true)
                .WithCurrentTimestamp()
                ;
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }

        // update this perhaps?
        [Command("serverInfo")]
        [Summary("see the server stats")]
        public async Task ServerInfo()
        {
            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            var activeCitizens = await db.ActiveServerCitizensAsync(Context, server.CitizenRole);
            await db.Entry<Server>(server).Collection(s => s.Members).LoadAsync();
            var builder = new EmbedBuilder()
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithTitle(Context.Guild.Name)
                .WithDescription(Context.Guild.Description)
                .WithColor(Color.LightOrange)
                .AddField("active citizens :", activeCitizens, true)
                .AddField("active users :", server.ActiveUsers, true)
                .AddField("participating members :", server.Members.Count, true)
                .AddField("points multiplier :", server.PointsMultiplier, true)
                .AddField("cooldown rate :", $"{server.Cooldown} seconds", true)
                .AddField("created :", Context.Guild.CreatedAt)
                ;
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }
        

        // hide from users
        [Command("setPointsMultiplier")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("set the server's points multiplier")]
        public async Task SetPointsMultiplier(uint multiplier)
        {
            const uint maxMultiplier = 20;
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            server.PointsMultiplier = multiplier < maxMultiplier ? multiplier : maxMultiplier;
            await db.SaveChangesAsync();
            var message = await Context.Channel.SendMessageAsync($"points multiplier set to {server.PointsMultiplier}");
            await this.CleanUp(Context, message);
        }

        // hide from users
        [Command("setCoolDown")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("set the server spam cooldown rate (seconds)")]
        public async Task SetCoolDown(uint seconds)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            server.Cooldown = seconds;
            await db.SaveChangesAsync();
            var message = await Context.Channel.SendMessageAsync($"cooldown set to {server.Cooldown}");
            await this.CleanUp(Context, message);
        }

        // can we make this better
        [Command("pfp")]
        [Summary("see your pfp, or someone else's, but bigger")]
        public async Task Pfp(SocketGuildUser user = null)
        {
            user ??= (Context.User as SocketGuildUser);
            var builder = new EmbedBuilder()
                .WithImageUrl(user.GetAvatarUrl()
                    ?? user.GetDefaultAvatarUrl())
                .WithTitle($"{user.Username}#{user.Discriminator}")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                ;
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }

        // make more intuitive, set a maximum
        [Command("purge")]
        [Summary("search the last n posts and delete any made by you")]
        public async Task Purge(int amount, SocketGuildUser user = null)
        {
            user ??= Context.User as SocketGuildUser;
            IUserMessage message = null;
            if (user.Id != Context.User.Id && user.Hierarchy >= (Context.User as SocketGuildUser).Hierarchy)
            {
                string reply = $"you don't have permission to purge {user.Username}'s posts";
                message = await Context.Channel.SendMessageAsync(reply);
                await this.CleanUp(Context, message);
                return;
            }
            IEnumerable<IMessage> messages = (await Context.Channel
                .GetMessagesAsync(limit: amount + 1)
                .FlattenAsync<IMessage>())
                .Skip(1)
                .Where(m => m.Author == user);
            foreach (IMessage msg in messages)
            {
                await Context.Channel.DeleteMessageAsync(msg);
                await Task.Delay(1100);
            }
            message = await Context.Channel.SendMessageAsync(
                $"{messages.Count()} messages have been purged");
            await this.CleanUp(Context, message);
        }

        // experimental
        [Command("makechan")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Make a channel")]
        public async Task MakeChan([Remainder] string arg)
        {
            var channel = await Context.Guild.CreateTextChannelAsync(name: arg);
            await Context.Channel.SendMessageAsync($"{channel.Mention}");
        }
    }
}
