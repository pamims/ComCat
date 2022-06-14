using Discord;
using Discord.Commands;
using ComCat.Services;
using ComCat.Services.Ballot;
using System;
using System.Collections.Generic;
/*using System.Text;
using System.Threading.Tasks;

namespace Liber.Modules
{
    public class TestModule : ModuleBase
    {
        private BallotService _ballots;
        public TestModule(BallotService ballots)
        {
            _ballots = ballots;
        }


        [Command("buttons")]
        public async Task Buttons()
        {
            var builder = new ComponentBuilder()
                .WithButton("Yea", "up-ballot-vote", ButtonStyle.Success, new Emoji("\uD83D\uDC4D"))
                .WithButton("Nay", "down-ballot-vote", ButtonStyle.Danger, new Emoji("\uD83D\uDC4E"));
            var msg = await Context.Channel.SendMessageAsync("this fucker has a button", component: builder.Build());
            //_ballots.AddBallot(new Ballot(msg.Id, 40, 10));
        }
    }
}
*/