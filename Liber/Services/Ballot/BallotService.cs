using Discord.WebSocket;
using ComCat.Extensions;
using ComCat.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComCat.Services.Ballot
{
    public class BallotService
    {
        private readonly List<AbstractBallot> _ballots;
        private readonly IServiceScopeFactory _scopeFactory;
        public BallotService(DiscordSocketClient client, IServiceScopeFactory scopeFactory)
        {
            client.ButtonExecuted += OnButtonExecuted;
            client.LatencyUpdated += UpdateBallots;
            _scopeFactory = scopeFactory;
            _ballots = new List<AbstractBallot>();

        }

        public void AddBallot(AbstractBallot ballot)
        {
            _ballots.Add(ballot);
        }

        public bool Contains<T>(Func<T, bool> predicate)
        {
            return _ballots.OfType<T>().Any<T>(predicate);
        }

        private async Task OnButtonExecuted(SocketMessageComponent button)
        {
            if (!button.Data.CustomId.EndsWith("-ballot-vote")) return;
            await button.DeferAsync();
            using var scope = _scopeFactory.GetRequiredScopedService<LDbContext>(out var db);
            var user = button.User as SocketGuildUser;
            var server = await db.GetServerAsync(user.Guild.Id);
            if (!user.Roles.Any(r => r.Id == server.CitizenRole))
            {
                return;
            }
            var ballot = _ballots.Find(b => b.MessageId == button.Message.Id);
            if (ballot == null || ballot.IsComplete) return;
            ballot.CastVote(user.Id, button.Data.CustomId == "up-ballot-vote");
            await button.Message.ModifyAsync(m => m.Embed = ballot.Embed());
        }

        private async Task UpdateBallots(int gar, int bage)
        {
            for (int i = _ballots.Count - 1; i >= 0; i--)
            {
                var ballot = _ballots[i];
                if (ballot.IsComplete)
                {
                    _ballots.Remove(ballot);
                    Console.WriteLine($"Ballot removed from list. {_ballots.Count} ballots still in list.");
                    await ballot.ExecuteBallotAsync();
                }
            }
        }
    }
}
