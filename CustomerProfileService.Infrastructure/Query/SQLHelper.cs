using MySql.Data.MySqlClient;
using CustomerProfileService.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CustomerProfileService.Infrastructure.Query;

public class SQLHelper
{
    private MySqlConnection? connection;
    private readonly IConfiguration _config;

    public SQLHelper(IConfiguration config)
    {
        _config = config;
    }

    private async Task<MySqlConnection> OpenSqlConnectionAsync()
    {
        var connectionString = _config.GetConnectionString("MySql");

        if (connection != null &&
            connection.State == System.Data.ConnectionState.Open &&
            connection.ConnectionString == connectionString)
        {
            return connection;
        }
        else
        {
            CloseSqlConnection();
        }

        connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public void CloseSqlConnection()
    {
        if (connection != null)
        {
            connection.Close();
            connection.Dispose();
            connection = null;
        }
    }

    //     1️⃣ SELECT – no parameters
    public async Task<MySqlDataReader> ExecuteQueryAsync(string query)
    {
        try
        {
            var conn = await OpenSqlConnectionAsync();
            var cmd = new MySqlCommand(query, conn);

            return (MySqlDataReader)await cmd.ExecuteReaderAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Database query execution failed.", ex);
        }
    }

    // 2️⃣ SELECT – with parameters
    public async Task<MySqlDataReader> ExecuteQueryAsync(string query, Action<MySqlCommand> param)
    {
        try
        {
            var conn = await OpenSqlConnectionAsync();
            var cmd = new MySqlCommand(query, conn);

            param(cmd);

            return (MySqlDataReader)await cmd.ExecuteReaderAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Database query execution failed.", ex);
        }
    }

    // 3️⃣ INSERT / UPDATE / DELETE – no return data
    public async Task<int> ExecuteNonQuery(string query, Action<MySqlCommand> param)
    {
        try
        {
            using var conn = await OpenSqlConnectionAsync();
            using var cmd = new MySqlCommand(query, conn);

            param(cmd);

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (MySqlException)
        {
            throw; // let service decide what to do
        }
        catch (Exception ex)
        {
            throw new Exception("Database operation failed.", ex);
        }
    }


    // public async Task<MySqlDataReader> FetchAsync(string query, Action<MySqlCommand>? param = null)
    // {
    //     var conn = await OpenSqlConnectionAsync();
    //     var cmd = new MySqlCommand(query, conn);

    //     param?.Invoke(cmd);

    //     return (MySqlDataReader)await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
    // }

    // public async Task ExecuteAsync(string query, Action<MySqlCommand> param)
    // {
    //     Console.WriteLine("Helper -> ");

    //     try
    //     {
    //         var conn = await OpenSqlConnectionAsync();

    //         var cmd = new MySqlCommand(query, conn);
    //         cmd.Connection = conn;

    //         param(cmd);

    //         await cmd.ExecuteNonQueryAsync();
    //     }
    //     catch (MySqlException ex)
    //     {
    //         throw new Exception("Database operation failed", ex);
    //     }
    // }


    public static string GetStringValue(MySqlDataReader reader, string columnName)
    {
        try
        {
            int index = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(index) ? reader.GetString(index) : string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logging exception for column: {columnName}");
            Console.WriteLine(ex);
            return string.Empty;
        }
    }

    public static int GetIntValue(MySqlDataReader reader, string columnName)
    {
        try
        {
            int index = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(index) ? reader.GetInt32(index) : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logging exception for column: {columnName}");
            Console.WriteLine(ex);
            return 0;
        }
    }

}
