using Microsoft.EntityFrameworkCore;
using SharedLibrary;

namespace IndexerService.Shards;

public class ShardedDbContext
{
    private readonly Dictionary<string, string> _shardConnections;

    public ShardedDbContext(List<string> shardConnections)
    {
        _shardConnections = new Dictionary<string, string>
        {
            { "shard1", shardConnections[0] },
            { "shard2", shardConnections[1] }
        };
    }

    public DbContextConfig GetShardContext(string user)
    {
        // Decide which shard to use
        string connectionString = user.StartsWith("A-M") ? _shardConnections["shard1"] : _shardConnections["shard2"];

        var optionsBuilder = new DbContextOptionsBuilder<DbContextConfig>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DbContextConfig(optionsBuilder.Options);
    }
}