using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace autofact
{
    public class Bdd
    {
        private readonly string _connectionString;

        // Default uses local MariaDB/MySQL; override by passing a connection string to the constructor
        public Bdd(string? connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Try to read from appsettings.json when no connection string provided
                try
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                        .Build();

                    var cfgConn = config.GetConnectionString("Default");
                    if (!string.IsNullOrWhiteSpace(cfgConn))
                    {
                        _connectionString = cfgConn;
                        return;
                    }
                }
                catch
                {
                    // fall back to hardcoded default below
                }

                // Default connection: local MariaDB/MySQL on 127.0.0.1 (adjust user/password as needed)
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = "192.168.56.200",
                    Port = 3306,
                    Database = "Autofact",
                    UserID = "app",
                    Password = "Demaindeslaube",
                    SslMode = MySqlSslMode.None,
                    ConnectionTimeout = 5
                };
                _connectionString = builder.ConnectionString;
            }
            else
            {
                _connectionString = connectionString;
            }
        }

        // Test a connection by opening and closing it within the given timeout
        public async Task<bool> TestConnectionAsync(int timeoutSeconds = 5)
        {
            try
            {
                var builder = new MySqlConnectionStringBuilder(_connectionString)
                {
                    ConnectionTimeout = (uint)timeoutSeconds
                };

                await using var conn = new MySqlConnection(builder.ConnectionString);
                await conn.OpenAsync();
                await conn.CloseAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Initialize DB using sql/init.sql (splits on "-- GO --" or on lines with just GO)
        public async Task InitializeDatabaseAsync()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "sql", "init.sql");
            if (!File.Exists(path))
                throw new FileNotFoundException("init.sql not found", path);

            var sql = await File.ReadAllTextAsync(path);

            // Split batches on GO lines (case-insensitive)
            var batches = Regex.Split(sql, "^\\s*GO\\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = batch;
                await cmd.ExecuteNonQueryAsync();
            }

            await conn.CloseAsync();
        }

        // Create and return a MySqlConnection (caller responsible for opening/disposing)
        public MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);

        // Simple helper to run non-query commands
        public async Task<int> ExecuteNonQueryAsync(string sql, IEnumerable<MySqlParameter>? parameters = null)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        // Simple helper to execute scalar
        public async Task<object?> ExecuteScalarAsync(string sql, IEnumerable<MySqlParameter>? parameters = null)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
            }

            return await cmd.ExecuteScalarAsync();
        }

        // New helpers for user management
        public async Task<(int id, string hash, bool actif)?> GetUserByEmailAsync(string email)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, mot_de_passe, actif FROM utilisateur WHERE email = @email LIMIT 1";
            cmd.Parameters.Add(new MySqlParameter("@email", email));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var hash = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var actif = !reader.IsDBNull(2) && reader.GetBoolean(2);
                await conn.CloseAsync();
                return (id, hash, actif);
            }

            await conn.CloseAsync();
            return null;
        }

        public async Task<bool> CreateUserAsync(string email, string passwordHash)
        {
            try
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO utilisateur (email, mot_de_passe, date_inscription, actif) VALUES (@email, @pwd, @date, 1)";
                cmd.Parameters.Add(new MySqlParameter("@email", email));
                cmd.Parameters.Add(new MySqlParameter("@pwd", passwordHash));
                cmd.Parameters.Add(new MySqlParameter("@date", DateTime.UtcNow.Date));
                await cmd.ExecuteNonQueryAsync();
                await conn.CloseAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
