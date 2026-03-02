using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;

namespace autofact
{
    internal class Bdd
    {
        // Keep connection string here for simplicity; in production move to configuration
        private readonly string connectionString = "Server=192.168.56.200;Database=AutoFact;User Id=app;Password=Demaindeslaube;";

        /// <summary>
        /// Test if the database is reachable.
        /// </summary>
        public async Task<bool> TestConnectionAsync(int timeoutSeconds = 5)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ConnectTimeout = timeoutSeconds
                };

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    return connection.State == System.Data.ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Synchronous wrapper for testing connection.
        /// </summary>
        public bool TestConnection(int timeoutSeconds = 5)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ConnectTimeout = timeoutSeconds
                };

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    return connection.State == System.Data.ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an open SqlConnection. Caller is responsible for disposing it.
        /// </summary>
        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection; // caller must Dispose()
        }

        /// <summary>
        /// Synchronous helper that returns an open SqlConnection. Caller must dispose.
        /// </summary>
        public SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        // Example helper to run a simple query and return a DataTable
        public async Task<DataTable> ExecuteQueryAsync(string sql, params SqlParameter[] parameters)
        {
            var dt = new DataTable();
            using (var conn = await GetOpenConnectionAsync())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }
    }
}
