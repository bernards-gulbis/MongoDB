using MongoDB.Driver;
using MongoDB.Models;

namespace MongoDB.DataAccess;

public class ChoreDataAccess
{
    private const string DatabaseName = "DemoDatabase";
    private const string ChoreCollection = "chore_chart";
    private const string UserCollection = "users";
    private const string ChoreHistoryCollection = "chore_history";

    private IMongoCollection<T> ConnectToMongo<T>(in string collection)
    {
        var userName = Environment.GetEnvironmentVariable("MDB_USERNAME");
        var password = Environment.GetEnvironmentVariable("MDB_PASSWORD");
        var host = Environment.GetEnvironmentVariable("MDB_HOST");
        var port = Environment.GetEnvironmentVariable("MDB_PORT");

        var connectionString = $@"mongodb://{userName}:{password}@{host}:{port}";
        var client = new MongoClient(connectionString);

        var db = client.GetDatabase(DatabaseName);
        return db.GetCollection<T>(collection);
    }

    public async Task<List<UserModel>> GetAllUsers()
    {
        var userCollection = ConnectToMongo<UserModel>(UserCollection);
        var result = await userCollection.FindAsync(_ => true);
        return result.ToList();
    }

    public async Task<List<ChoreModel>> GetAllChores()
    {
        var choreCollection = ConnectToMongo<ChoreModel>(ChoreCollection);
        var result = await choreCollection.FindAsync(_ => true);
        return result.ToList();
    }

    public async Task<List<ChoreHistoryModel>> GetAllChoreHistories()
    {
        var chorehistoryCollection = ConnectToMongo<ChoreHistoryModel>(ChoreHistoryCollection);
        var result = await chorehistoryCollection.FindAsync(_ => true);
        return result.ToList();
    }

    public async Task<List<ChoreModel>> GetAllChoresForAUser(UserModel user)
    {
        var choreCollection = ConnectToMongo<ChoreModel>(ChoreCollection);
        var result = await choreCollection.FindAsync(c => c.AssignedTo.Id == user.Id);
        return result.ToList();
    }

    public Task CreateUser(UserModel user)
    {
        var userCollection = ConnectToMongo<UserModel>(UserCollection);
        return userCollection.InsertOneAsync(user);
    }

    public Task CreateChore(ChoreModel chore)
    {
        var choreCollection = ConnectToMongo<ChoreModel>(ChoreCollection);
        return choreCollection.InsertOneAsync(chore);
    }

    public Task UpdateChore(ChoreModel chore)
    {
        var choreCollection = ConnectToMongo<ChoreModel>(ChoreCollection);
        var filter = Builders<ChoreModel>.Filter.Eq("Id", chore.Id);
        return choreCollection.ReplaceOneAsync(filter, chore, new ReplaceOptions { IsUpsert = true });
    }

    public Task DeleteChore(ChoreModel chore)
    {
        var choreCollection = ConnectToMongo<ChoreModel>(ChoreCollection);
        return choreCollection.DeleteOneAsync(c => c.Id == chore.Id);
    }

    public async Task CompleteChore(ChoreModel chore)
    {
        var choreCollection = ConnectToMongo<ChoreModel>(ChoreCollection);
        var filter = Builders<ChoreModel>.Filter.Eq("Id", chore.Id);
        await choreCollection.ReplaceOneAsync(filter, chore);

        var chorehistoryCollection = ConnectToMongo<ChoreHistoryModel>(ChoreHistoryCollection);
        await chorehistoryCollection.InsertOneAsync(new ChoreHistoryModel(chore));
    }

    public async Task CompleteChoreWithTransaction(ChoreModel chore)
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("MDB_CONNECTION_STRING"));
        using var session = await client.StartSessionAsync();

        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(DatabaseName);
            var choreCollection = db.GetCollection<ChoreModel>(ChoreCollection);
            var filter = Builders<ChoreModel>.Filter.Eq("Id", chore.Id);
            await choreCollection.ReplaceOneAsync(filter, chore);

            var chorehistoryCollection = db.GetCollection<ChoreHistoryModel>(ChoreHistoryCollection);
            await chorehistoryCollection.InsertOneAsync(new ChoreHistoryModel(chore));

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            Console.WriteLine(ex.Message);
        }
    }
}
