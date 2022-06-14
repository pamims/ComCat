using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;

namespace ComCat.Services.Ballot
{
    class ServerNameBallot : AbstractBallot
    {
        private readonly string _name;
        private readonly SocketGuild _guild;
        public SocketGuild Guild { get { return _guild; } }

        public ServerNameBallot(ulong messageId, uint seconds, int requiredVotes,
            IMessageChannel channel, string name, SocketGuild guild)
            : base(messageId, seconds, requiredVotes, channel)
        {
            _name = name;
            _guild = guild;
            _failureMessage = $"not enough votes cast to change `{guild.Name}` to `{name}`";
            _successMessage = $"congratulations, the server has been changed to `{name}`";
            _exceptionMessage = $"sorry, the server name would have been changed to `{name}`,"
                    + " but something went wrong...";
        }

        public override Embed Embed()
        {
            var embed = Embed(_guild.IconUrl,
                    "change server name",
                    $"vote to change server name to \n**{_name}**",
                    _votes.Count,
                    _requiredVotes);
            return embed.Build();
        }

        public override async Task BallotActionAsync()
        {
            await _guild.ModifyAsync(g => g.Name = _name);
            return;
        }
    }
}
