using Discord;
using Discord.WebSocket;
using ComCat.Extensions;
using ComCat.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ComCat.Services
{
    public class PassiveService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceScopeFactory _scopeFactory;
        private DateTime _date;

        public PassiveService(DiscordSocketClient client, IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _client.MessageReceived += OnMessageReceived;
            _client.UserJoined += OnUserJoined;
            _scopeFactory = scopeFactory;
            _date = DateTime.Today;
        }

        private async Task OnUserJoined(SocketGuildUser user)
        {
            var channel = (user.Guild.DefaultChannel);
            if (channel == null) return;
            var message = await channel
                .SendMessageAsync($"welcome to {user.Guild.Name} {user.Mention}");
            await Task.Delay(10000);
            await channel.DeleteMessageAsync(message);
        }

        private async Task OnMessageReceived(SocketMessage arg)
        {

            using var scope =
                _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            if (DateTime.Compare(DateTime.Today, _date) > 0)
            {
                int daysPassed = (int)(DateTime.Today - _date).TotalDays;
                await db.UpdateServerMembersAsync(daysPassed);
                _date = DateTime.Today;
                await db.SaveChangesAsync();
            }
            if (!(arg is SocketUserMessage msg)) return;
            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;
            if (!(msg.Author is SocketGuildUser user)) return;
            var member = await db.GetServerMemberAsync(user.Guild.Id, user.Id);
            await db.Entry(member).Reference(m => m.Server).LoadAsync();
            if ((DateTime.Now - member.LastSeen).TotalSeconds > member.Server.Cooldown)
            {
                member.LastSeen = DateTime.Now;
                member.Points += member.Server.PointsMultiplier;
            }
            member.NumPosts++;
            await db.SaveChangesAsync();
        }
    }
}
