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
    public class ServerCommandsModule : ModuleBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BallotService _ballots;
        private const uint _voteTimeout = 3600;
        private readonly MessageComponent _voteButtons;
        private readonly HttpClient _httpClient;

        public ServerCommandsModule(IServiceScopeFactory scopeFactory, BallotService ballots,
            HttpClient httpClient)
            : base()
        {
            _scopeFactory = scopeFactory;
            _ballots = ballots;
            _httpClient = httpClient;
            _voteButtons = new ComponentBuilder()
                .WithButton("Yea", "up-ballot-vote", ButtonStyle.Success, new Emoji("\uD83D\uDC4D"))
                .WithButton("Nay", "down-ballot-vote", ButtonStyle.Danger, new Emoji("\uD83D\uDC4E"))
                .Build();
        }



        [Command("ban")]
        [Summary("trade your citizen role to ban another citizen or ban a non-citizen for free")]
        public async Task Ban(SocketGuildUser user = null, [Remainder] string reason = "no reason given.")
        {
            if (user == null)
            {
                var message = await Context.Channel.SendMessageAsync("specify whom to ban");
                await this.CleanUp(Context, message);
                return;
            }

            if (!(Context.User is SocketGuildUser actor)) return;

            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);

            if (!actor.Roles.Any<SocketRole>(r => r.Id == server.CitizenRole))
            {
                var message = await Context.Channel.SendMessageAsync("Only citizens can ban people");
                await this.CleanUp(Context, message);
                return;
            }

            if (user.Roles.Any<SocketRole>(r => r.Id == server.CitizenRole))
                await actor.RemoveRoleAsync(server.CitizenRole, new RequestOptions
                {
                    AuditLogReason = $"Issued ban for {user.Username}."
                });
            await user.BanAsync(reason: $"{reason}");
            await Context.Channel.SendMessageAsync($"{actor.Mention} banned {user.Mention}\n"
                + $"reason given : {reason}");
            await this.CleanUp(Context);
        }

        [Command("promote")]
        [Summary("grant citizenship to a non-citizen")]
        public async Task Promote(IGuildUser user)
        {
            if (!(Context.User is IGuildUser citizen)) return;
            if (user.IsBot)
            {
                await Context.Channel.SendMessageAsync("robots can't be citizens :(");
                return;
            }
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            if (!citizen.RoleIds.Contains<ulong>(server.CitizenRole))
            {
                await Context.Channel.SendMessageAsync("you can't make a citizenship proposal if you aren't a citizen");
                return;
            }
            if (user.RoleIds.Contains<ulong>(server.CitizenRole))
            {
                await Context.Channel.SendMessageAsync($"{user.Username} is already a citizen");
                return;
            }
            if (_ballots.Contains<PromoteBallot>(b => b.User == user))
            {
                await Context.Channel.SendMessageAsync($"there is already an open ballot for {user.Username}");
                return;
            }
            int activeCitizens = await db.ActiveServerCitizensAsync(Context, server.CitizenRole);
            // round(4.4 * ln(x) - 6)
            int requiredVotes = (int)Math.Round(4.4 * Math.Log(activeCitizens) - 6);
            if (requiredVotes < 2)
            {
                try
                {
                    await user.AddRoleAsync(server.CitizenRole, new RequestOptions
                    {
                        AuditLogReason = $"granted citizenship by {user.Username}#{user.Discriminator} ({user.Id})"
                    });
                    await Context.Channel.SendMessageAsync($"{user.Username} was granted citizenship by {citizen.Username}");
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync("couldn't promote the user for some reason\n"
                        + $"my error message says: {e.Message}");
                    throw;
                }
                return;
            }
            var msg = await Context.Channel.SendMessageAsync("generating ballot...");
            var ballot = new PromoteBallot(msg.Id, _voteTimeout, requiredVotes, Context.Channel, server.CitizenRole, user);
            await AddBallotAsync(ballot, msg);
        }

        [Command("changeServerName")]
        [Summary("change the server's name")]
        public async Task ChangeServerName([Remainder] string name)
        {
            if (!(Context.User is IGuildUser citizen)) return;
            name = name.Length < 80 ? name : name.Substring(0, 80);
            if (string.IsNullOrEmpty(name))
            {
                await Context.Channel.SendMessageAsync("name can't be blank :(");
                return;
            }
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            if (!citizen.RoleIds.Contains<ulong>(server.CitizenRole))
            {
                await Context.Channel.SendMessageAsync("you can't make a change proposal if you aren't a citizen");
                return;
            }
            if (Context.Guild.Name == name)
            {
                await Context.Channel.SendMessageAsync($"the server is already named `{name}`");
                return;
            }
            if (_ballots.Contains<ServerNameBallot>(b => b.Guild == Context.Guild))
            {
                await Context.Channel.SendMessageAsync($"there is already an open name ballot for `{Context.Guild.Name}`");
                return;
            }
            int activeCitizens = await db.ActiveServerCitizensAsync(Context, server.CitizenRole);
            int requiredVotes = (int)Math.Round(4.4 * Math.Log(activeCitizens) - 6);
            if (requiredVotes < 2)
            {
                try
                {
                    await Context.Guild.ModifyAsync(g => g.Name = name, new RequestOptions
                    {
                        AuditLogReason = $"name changed by {citizen.Username}#{citizen.Discriminator} ({citizen.Id})"
                    });
                    await Context.Channel.SendMessageAsync($"server name changed to {name} by {citizen.Username}");
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync("couldn't change server name for some reason\n"
                        + $"my error message says: {e.Message}");
                    throw;
                }
                return;
            }
            var msg = await Context.Channel.SendMessageAsync("generating ballot...");
            // 0 in place of requiredVotes because its a 24 hour ballot
            var ballot = new ServerNameBallot(msg.Id, 86400, 0, Context.Channel, name, Context.Guild as SocketGuild);
            await AddBallotAsync(ballot, msg);
        }

        #region InProgress
        /*
        [Command("changeServerIcon")]
        [Summary("change the server's icon")]
        public async Task ChangeServerIcon()
        {

            if (!(Context.User is SocketGuildUser citizen)) return;
            IAttachment attachment = null;
            try
            {
                attachment = Context.Message.Attachments
                    .Single<IAttachment>();
                if (attachment.Width == null || attachment.Height == null)
                    throw new FormatException("attachment is not an image");
                if (attachment.Width != 512 && attachment.Height != 512)
                    throw new FormatException("incorrect image size");
                string filename = attachment.Filename;
                if (!filename.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase)
                    || !filename.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)
                    || !filename.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase)
                    || !filename.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase))
                    throw new FormatException("incorrect file format");
            }
            catch
            {
                await Context.Channel.SendMessageAsync(
                    "please provide a single 512 x 512 image.\n"
                    + "supported formats: .png, .jpg, .gif");
                return;
            }
            string url = attachment.Url;
            using Stream imageStream = await _httpClient.GetStreamAsync(url);
            Image image = new Image(imageStream);
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var server = await db.GetServerAsync(Context.Guild.Id);
            if (!citizen.Roles.Any<SocketRole>(r => r.Id == server.CitizenRole))
            {
                await Context.Channel.SendMessageAsync("you can't make a change proposal if you aren't a citizen");
                return;
            }
            if (_ballots.Contains<ServerIconBallot>(b => b.Guild == Context.Guild))
            {
                await Context.Channel.SendMessageAsync($"there is already an open icon ballot for `{Context.Guild.Name}`");
                return;
            }
            await db.Entry(server).Collection(s => s.Members).LoadAsync();
            int activeCitizens = server.Members.Count<ServerMember>(m => m.IsActive);
            int requiredVotes = (int)Math.Round(4.4 * Math.Log(activeCitizens) - 6);
            if (requiredVotes < 2)
            {
                try
                {
                    await Context.Guild.ModifyAsync(g => g.Name = name, new RequestOptions
                    {
                        AuditLogReason = $"name changed by {citizen.Username}#{citizen.Discriminator} ({citizen.Id})"
                    });
                    await Context.Channel.SendMessageAsync($"server name changed to {name} by {citizen.Username}");
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync("couldn't change server name for some reason\n"
                        + $"my error message says: {e.Message}");
                    throw;
                }
                return;
            }
            var msg = await Context.Channel.SendMessageAsync("generating ballot...");
            var ballot = new ServerNameBallot(msg.Id, _voteTimeout, requiredVotes, Context.Channel, name, Context.Guild as SocketGuild);
            await AddBallotAsync(ballot, msg);
        }
        */
        #endregion

        private async Task AddBallotAsync(AbstractBallot ballot, IUserMessage msg)
        {
            _ballots.AddBallot(ballot);
            var embed = ballot.Embed();
            await msg.ModifyAsync(m =>
            {
                m.Content = null;
                m.Embed = embed;
                m.Components = _voteButtons;
            });
            await this.CleanUp(Context);
        }
    }
}
