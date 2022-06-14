using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;

namespace ComCat.Services.Ballot
{
    class PromoteBallot : AbstractBallot
    {
        private readonly ulong _roleId;
        private readonly IGuildUser _user;
        public IGuildUser User { get { return _user; } }

        public PromoteBallot(ulong messageId, uint seconds, int requiredVotes,
            IMessageChannel channel, ulong roleId, IGuildUser user)
            : base(messageId, seconds, requiredVotes, channel)
        {
            _roleId = roleId;
            _user = user;
            _failureMessage = $"not enough votes cast to promote {_user.Mention} to citizen";
            _successMessage = $"congratulations {_user.Mention}, you are a citizen";
            _exceptionMessage = $"sorry, {_user.Mention}, you would have become a citizen,"
                    + " but something went wrong...";
        }

        public override Embed Embed()
        {
            var embed = Embed(_user.GetAvatarUrl()
                    ?? _user.GetDefaultAvatarUrl(),
                    "grant citizenship",
                    $"vote to promote {_user.Mention} to citizenship",
                    _votes.Count,
                    _requiredVotes);
            return embed.Build();
        }

        public override async Task BallotActionAsync()
        {
            await _user.AddRoleAsync(_roleId);
            return;
        }
    }
}
