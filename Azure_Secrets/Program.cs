using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Azure.Identity;

namespace Azure_Secrets
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Testing connection...");
            await TestConnection();
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
            Console.WriteLine("Done.");
        }

        #region Connection To Azure SQL Database

        private static bool _isProviderRegistered;

        static Program()
        {
            _isProviderRegistered = false;
        }

        private static ClientSecretCredential GetClientSecretCredential()
        {
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            {
                throw new InvalidOperationException("Azure client ID, secret or tenant ID is not set in the environment variables.");
            }
            
            return new ClientSecretCredential(tenantId, clientId, clientSecret);
        }

        private static void RegisterEncryptionProvider(TokenCredential tokenCredential)
        {
            if (_isProviderRegistered) return;
            
            var azureKeyVaultProvider = new SqlColumnEncryptionAzureKeyVaultProvider(tokenCredential);
            var customProviders = new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>{
                { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, azureKeyVaultProvider }
            };
            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders);
            _isProviderRegistered = true;
        }

        private static SqlConnectionStringBuilder GetConnectionStringBuilder()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string not found in environment variables.");
            }

            return new SqlConnectionStringBuilder(connectionString);
        }

        private static async Task<SqlConnection> GetConnection()
        {
            var tokenCredential = GetClientSecretCredential();
            RegisterEncryptionProvider(tokenCredential);
            var builder = GetConnectionStringBuilder();
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static async Task TestConnection()
        {
            await using var connection = await GetConnection();

            Console.WriteLine(connection.State == System.Data.ConnectionState.Open
                ? "Connected successfully."
                : "Connection failed.");
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
            {
                Console.WriteLine($"{reader["Id"]} - {reader["Name"]} - {reader["Email"]} - {reader["Password"]}");
            }
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
        
        #endregion
    }
}