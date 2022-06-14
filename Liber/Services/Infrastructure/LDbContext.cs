using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComCat.Services.Infrastructure
{
    public class LDbContext : DbContext
    {
        public DbSet<Server> Servers { get; set; }
        public DbSet<ServerMember> ServerMembers { get; set; }
        public DbSet<Member> Members { get; set; }

        public LDbContext(DbContextOptions<LDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ServerMember>()
                .HasKey(sm => new { sm.ServerID, sm.MemberID });
            builder.Entity<Server>()
                .HasMany(s => s.Members)
                .WithOne(m => m.Server);
            builder.Entity<Member>()
                .HasMany(m => m.Servers)
                .WithOne(s => s.Member);
        }
    }

    public class Server
    {
        public ulong ServerID { get; set; }


        public uint ActiveUsers { get; set; }
        public ulong CitizenRole { get; set; }
        public List<ServerMember> Members { get; set; } = new List<ServerMember>();
        public uint PointsMultiplier { get; set; }
        public uint Cooldown { get; set; }
    }

    public class ServerMember
    {
        public Server Server { get; set; }
        public ulong ServerID { get; set; }
        public Member Member { get; set; }
        public ulong MemberID { get; set; }


        [Column(TypeName = "decimal(5, 2)")]
        public decimal SmoothMovingAverage { get; set; }
        public uint NumPosts { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastSeen { get; set; }
        public uint Points { get; set; }
    }

    public class Member
    {
        public ulong MemberID { get; set; }


        public List<ServerMember> Servers { get; set; } = new List<ServerMember>();

        [Column(TypeName = "decimal(18, 10)")]
        public decimal Coins { get; set; }
    }
}
