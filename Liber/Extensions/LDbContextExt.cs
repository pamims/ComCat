using ComCat.Services.Infrastructure;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComCat.Extensions
{
    public static class LDbContextExt
    {
        public static async Task UpdateServerMembersAsync(this LDbContext context, int numDays = 1)
        {
            const decimal alpha = 0.2M;
            const decimal beta = 1M - alpha;
            const decimal activeLimit = 12M;
            const decimal inactiveLimit = 9M;
            const decimal maxPosts = 100M;
            //next_ma = input * alpha + (1-alpha) * current_ma
            var servers = context.Servers.Include(s => s.Members);
            foreach (Server s in servers)
            {
                s.ActiveUsers = (uint)s.Members.Count<ServerMember>(m =>
                {
                    var posts = (decimal)m.NumPosts;
                    posts = maxPosts < posts ? maxPosts : posts;
                    m.NumPosts = 0;
                    m.SmoothMovingAverage = posts * alpha + beta * m.SmoothMovingAverage;
                    m.SmoothMovingAverage *= beta.Pow(numDays - 1);
                    if (m.IsActive)
                    {
                        if (m.SmoothMovingAverage < inactiveLimit)
                        {
                            m.IsActive = false;
                        }
                    }
                    else
                    {
                        if (m.SmoothMovingAverage > activeLimit)
                            m.IsActive = true;
                    }
                    return m.IsActive;
                });
            }
            await context.SaveChangesAsync();
        }

        public static async Task<ServerMember> MakeServerMemberAsync(
            this LDbContext context, ulong serverId, ulong memberId)
        {
            if (await context.FindAsync<Server>(serverId) == null)
                await context.AddAsync(new Server { ServerID = serverId });
            if (await context.FindAsync<Member>(memberId) == null)
                await context.AddAsync(new Member { MemberID = memberId });

            var serverMember = new ServerMember
            {
                ServerID = serverId,
                MemberID = memberId
            };
            await context.AddAsync(serverMember);
            return serverMember;
        }

        public static async Task<Server> GetServerAsync(this LDbContext context, ulong serverId)
        {
            var server = await context.FindAsync<Server>(serverId);
            if (server == null)
            {
                await context.AddAsync(new Server { ServerID = serverId });
                server = await context.FindAsync<Server>(serverId);
            }
            return server;
        }

        public static async Task<ServerMember> GetServerMemberAsync(
            this LDbContext context, ulong serverId, ulong memberId)
        {
            var serverMember = await context.FindAsync<ServerMember>(serverId, memberId)
                ?? await context.MakeServerMemberAsync(serverId, memberId);
            return serverMember;
        }

        public static async Task ClearTablesAsync(this LDbContext context)
        {
            const string command = "DELETE FROM ";
            using var transaction = await context.Database.BeginTransactionAsync();
            await context.Database.ExecuteSqlRawAsync($"{command}{nameof(context.Servers)}");
            await context.Database.ExecuteSqlRawAsync($"{command}{nameof(context.ServerMembers)}");
            await context.Database.ExecuteSqlRawAsync($"{command}{nameof(context.Members)}");
            await transaction.CommitAsync();
        }

        public static async Task<int> ActiveServerCitizensAsync(this LDbContext db,
            ICommandContext context, ulong citizenRole)
        {
            List<ServerMember> citizens = await db.GetCitizensAsync(context, citizenRole)
                .ToListAsync<ServerMember>();
            return citizens.Count<ServerMember>(m => m.IsActive);
        }

        public static async IAsyncEnumerable<ServerMember> GetCitizensAsync(this LDbContext db,
            ICommandContext context, ulong citizenRole)
        {
            var users = await context.Guild.GetUsersAsync();
            var citizens = users
                .Where<IGuildUser>(u => u.RoleIds.Contains<ulong>(citizenRole));
            foreach (IGuildUser user in citizens)
            {
                ServerMember member = await db.GetServerMemberAsync(user.GuildId, user.Id);
                yield return member;
            }
        }
    }
}
