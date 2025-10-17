using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GotsThorlabs.Database.EntityRepo;

public class ThorlabsDbContextFactory : IDesignTimeDbContextFactory<ThorlabsDbContext>
{
    public ThorlabsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ThorlabsDbContext>();

        var contentRoot = Directory.GetCurrentDirectory();
        var dbDir = Path.Combine(contentRoot, "database");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "app.sqlite");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new ThorlabsDbContext(optionsBuilder.Options);
    }
}
