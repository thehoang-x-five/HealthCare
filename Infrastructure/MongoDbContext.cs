using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HealthCare.Infrastructure
{
    /// <summary>
    /// MongoDB context service managing MongoDB client lifecycle and database access.
    /// Implements singleton pattern with graceful degradation for connection failures.
    /// </summary>
    public interface IMongoDbContext
    {
        IMongoDatabase? Database { get; }
        bool IsConnected { get; }
        IMongoCollection<T> GetCollection<T>(string collectionName);
    }

    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoClient? _client;
        private readonly IMongoDatabase? _database;
        private readonly ILogger<MongoDbContext> _logger;
        private readonly bool _isConnected;

        public MongoDbContext(IConfiguration config, ILogger<MongoDbContext> logger)
        {
            _logger = logger;

            try
            {
                var connectionString = config["MongoDb:ConnectionString"];
                var databaseName = config["MongoDb:DatabaseName"];

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogWarning("MongoDB ConnectionString not configured - running in MySQL-only mode");
                    _isConnected = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    _logger.LogWarning("MongoDB DatabaseName not configured - running in MySQL-only mode");
                    _isConnected = false;
                    return;
                }

                _client = new MongoClient(connectionString);
                _database = _client.GetDatabase(databaseName);

                // Verify connection with ping command
                _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                _isConnected = true;

                _logger.LogInformation(
                    "MongoDB connected successfully: {Database} at {Server}",
                    databaseName,
                    connectionString.Split('@').LastOrDefault() ?? "localhost");
            }
            catch (MongoException ex)
            {
                _isConnected = false;
                _logger.LogWarning(ex,
                    "MongoDB connection failed - application will run in MySQL-only mode");
            }
            catch (TimeoutException ex)
            {
                _isConnected = false;
                _logger.LogWarning(ex,
                    "MongoDB connection timeout - application will run in MySQL-only mode");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning(ex,
                    "Unexpected error connecting to MongoDB - application will run in MySQL-only mode");
            }
        }

        public IMongoDatabase? Database => _database;

        public bool IsConnected => _isConnected;

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            if (!_isConnected || _database == null)
                throw new InvalidOperationException("MongoDB not connected");
            return _database.GetCollection<T>(collectionName);
        }
    }
}
