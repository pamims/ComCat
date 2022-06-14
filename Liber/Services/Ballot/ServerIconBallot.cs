using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;

namespace ComCat.Services.Ballot
{
    class ServerIconBallot : AbstractBallot
    {
        private readonly string _url;
        private readonly Image _img;
        private readonly SocketGuild _guild;
        public SocketGuild Guild { get { return _guild; } }

        public ServerIconBallot(ulong messageId, uint seconds, int requiredVotes,
            IMessageChannel channel, string url, Image img, SocketGuild guild)
            : base(messageId, seconds, requiredVotes, channel)
        {
            _url = url;
            _img = img;
            _guild = guild;
            _failureMessage = $"not enough votes cast to change the server icon";
            _successMessage = $"congratulations, the server icon has been changed";
            _exceptionMessage = $"sorry, the server icon would have been changed,"
                    + " but something went wrong...";
        }

        public override Embed Embed()
        {
            var embed = Embed(_guild.IconUrl,
                    "change server icon",
                    $"vote to change server icon",
                    _votes.Count,
                    _requiredVotes);
            embed.WithImageUrl(_url);
            return embed.Build();
        }

        public override async Task BallotActionAsync()
        {
            await _guild.ModifyAsync(g => g.Icon = _img);
            return;
        }
    }
}
