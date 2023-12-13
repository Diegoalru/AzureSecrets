using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;

namespace Azure_Secrets;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("~ Azure Secrets~");
        Console.WriteLine("Getting users...");
        await GetUsers();
        Console.WriteLine("Adding user...");
        await AddUser();
        Console.WriteLine("Getting users...");
        await GetUsers();
        Console.WriteLine("Updating user...");
        await UpdateUser();
        Console.WriteLine("Getting users...");
        await GetUsers();
        Console.WriteLine("Deleting user...");
        await DeleteUser();
        Console.WriteLine("Getting users...");
        await GetUsers();
        Console.WriteLine("Resetting identity...");
        await ResetIdentity();
        Console.WriteLine("Done.");
    }

    #region Connection To Azure SQL Database

    /// <summary>
    ///     Indicates whether the Azure Key Vault provider has been registered.
    /// </summary>
    private static bool _isProviderRegistered;

    static Program()
    {
        _isProviderRegistered = false;
    }

    /// <summary>
    ///     Retrieves the client secret credential for Azure authentication.
    /// </summary>
    /// <returns>
    ///     The client secret credential required for Azure authentication.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the Azure client ID, secret, or tenant ID is not set in the environment variables.
    /// </exception>
    private static ClientSecretCredential GetClientSecretCredential()
    {
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            throw new InvalidOperationException(
                "Azure client ID, secret or tenant ID is not set in the environment variables.");

        return new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    /// <summary>
    ///     Registers an encryption provider using the specified token credential.
    /// </summary>
    /// <param name="tokenCredential">
    ///     The token credential to use for authentication.
    /// </param>
    private static void RegisterEncryptionProvider(TokenCredential tokenCredential)
    {
        if (_isProviderRegistered) return;

        var azureKeyVaultProvider = new SqlColumnEncryptionAzureKeyVaultProvider(tokenCredential);
        var customProviders = new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
        {
            { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, azureKeyVaultProvider }
        };
        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders);
        _isProviderRegistered = true;
    }

    /// <summary>
    ///     Retrieves the connection string builder for the database connection.
    /// </summary>
    /// <returns>
    ///     The SqlConnectionStringBuilder object representing the connection string.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the connection string is not found in the environment variables.
    /// </exception>
    private static SqlConnectionStringBuilder GetConnectionStringBuilder()
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string not found in environment variables.");

        return new SqlConnectionStringBuilder(connectionString);
    }

    /// <summary>
    ///     Retrieves a database connection asynchronously.
    /// </summary>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result is a SqlConnection object.
    /// </returns>
    private static async Task<SqlConnection> GetConnection()
    {
        var tokenCredential = GetClientSecretCredential();
        RegisterEncryptionProvider(tokenCredential);
        var builder = GetConnectionStringBuilder();
        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
    
    #endregion

    #region CRUD Operations
    
    private static async Task GetUsers()
    {
        await using var connection = await GetConnection();

        const string sql = "SELECT * FROM Users";
        var command = new SqlCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            Console.WriteLine($"{reader["Id"]} - {reader["Name"]} - {reader["Email"]} - {reader["Password"]}");
    }
    
    private static async Task AddUser()
    {
        await using var connection = await GetConnection();

        const string sql = "INSERT INTO Users (Name, Email, Password) VALUES (@Name, @Email, @Password)";
        var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", "Diego Rubí");
        command.Parameters.AddWithValue("@Email", "drubis628@ulacit.ed.cr");
        command.Parameters.AddWithValue("@Password", "123456");
        await command.ExecuteNonQueryAsync();
    }

    private static async Task UpdateUser()
    {
        await using var connection = await GetConnection();

        const string sql = "UPDATE Users SET Name = @Name, Email = @Email, Password = @Password WHERE Id = @Id";
        var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", 2);
        command.Parameters.AddWithValue("@Name", "Diego Rubí Salas");
        command.Parameters.AddWithValue("@Email", "drubis628@ulacit.ed.cr");
        command.Parameters.AddWithValue("@Password", "654321");
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteUser()
    {
        await using var connection = await GetConnection();

        const string sql = "DELETE FROM Users WHERE Id = @Id";
        var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", 2);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Resets the identity column of the 'Users' table in the database.
    /// </summary>
    /// <returns>
    ///     A Task representing the asynchronous operation.
    /// </returns>
    private static async Task ResetIdentity()
    {
        await using var connection = await GetConnection();

        const string sql = "DBCC CHECKIDENT ('[dbo].[Users]', RESEED, 1);";
        var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #endregion
}