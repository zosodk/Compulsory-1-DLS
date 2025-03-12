using Microsoft.EntityFrameworkCore;
using SharedLibrary;

namespace IndexerService.Shards;

public class ShardedDbContext
{
    private readonly IConfiguration _configuration;

    public ShardedDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbContextConfig GetShardContext(string user)
    {
        string connectionString = user.StartsWith("A-M") ? 
            "Host=postgres_shard_1;Database=shard1db;" :
            "Host=postgres_shard_2;Database=shard2db;";

        var optionsBuilder = new DbContextOptionsBuilder<DbContextConfig>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DbContextConfig(optionsBuilder.Options, _configuration);
    }
}