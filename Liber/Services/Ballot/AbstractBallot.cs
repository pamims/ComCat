using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComCat.Services.Ballot
{
    public abstract class AbstractBallot
    {
        protected List<Vote> _votes;
        private readonly ulong _messageId;
        private readonly IMessageChannel _channel;
        private readonly DateTime _expiration;
        protected readonly int _requiredVotes;
        protected string _failureMessage;
        protected string _successMessage;
        protected string _exceptionMessage;
        public ulong MessageId { get { return _messageId; } }
        public bool IsComplete
        {
            get
            {
                var now = DateTime.Now;
                return ((now >= _expiration
                    && _votes.Count > _requiredVotes)
                    || (now - _expiration).TotalHours >= 24);
            }
        }

        public AbstractBallot(ulong messageId, uint seconds, int requiredVotes,
            IMessageChannel channel)
        {
            _expiration = DateTime.Now.AddSeconds(seconds);
            _requiredVotes = requiredVotes;
            _messageId = messageId;
            _votes = new List<Vote>();
            _channel = channel;
        }

        public abstract Embed Embed();
        public abstract Task BallotActionAsync();

        public static EmbedBuilder Embed(string imageUrl, string title, string description,
            int votes, int requiredVotes)
        {
            string fieldName = "votes";
            string fieldVotes = $"{votes}";
            if (requiredVotes > 0)
            {
                fieldName += " / required";
                fieldVotes += $" / {requiredVotes}";
            }
            fieldName += " :";
            return new EmbedBuilder()
                .WithThumbnailUrl(imageUrl)
                .WithTitle(title)
                .WithDescription(description)
                .AddField(fieldName, fieldVotes)
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp();
        }

        public void CastVote(ulong userId, bool upVote)
        {
            var vote = _votes.Find(v => v.UserId == userId);
            if (vote == null)
            {
                _votes.Add(new Vote
                {
                    UserId = userId,
                    UpVote = upVote
                });
                return;
            }
            vote.UpVote = upVote;
        }

        public async Task ExecuteBallotAsync()
        {
            var ballotMessage = await _channel.GetMessageAsync(_messageId);
            await (ballotMessage as SocketUserMessage)
                .ModifyAsync(m => m.Components = null);
            if (_votes.Count < _requiredVotes)
            {
                await _channel.SendMessageAsync(_failureMessage);
                return;
            }
            int count = _votes.Count<Vote>(v => v.UpVote);
            string response = _failureMessage ?? "ballot failed";
            if (count > _votes.Count - count)
            {
                try
                {
                    await BallotActionAsync();
                    response = _successMessage ?? "ballot passed";
                }
                catch (Exception e)
                {
                    await _channel.SendMessageAsync(
                        (_exceptionMessage ?? "ballot failed on error")
                        + $"\nerror message: '{e.Message}'");
                    throw;
                }
            }
            await _channel.SendMessageAsync(response
                + $"\n{count} / {_votes.Count} voted in favor");
            return;
        }
    }
}
