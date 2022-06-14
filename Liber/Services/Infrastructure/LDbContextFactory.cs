using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

// This class only exists for and gets used when using the CLI/EF dev tools

namespace ComCat.Services.Infrastructure
{
    class LDbContextFactory : IDesignTimeDbContextFactory<LDbContext>
    {
        public LDbContext CreateDbContext(string[] args)
        {
            
            var folder = Environment.SpecialFolder.ApplicationData;
            var path = Environment.GetFolderPath(folder);
            var options = new DbContextOptionsBuilder<LDbContext>()
                .UseSqlite($"Data Source={path}"
                        + $"{Path.DirectorySeparatorChar}Liber.db")
                .Options;
            return new LDbContext(options);
        }
    }
}
